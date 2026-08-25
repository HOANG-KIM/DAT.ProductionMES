using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// Implementation <see cref="IPackingProgressReportService"/> (US-26/FR-26, 24/08/2026; AC1-AC5 viết lại toàn bộ
/// 25/08/2026 sang mô hình tra cứu theo Lot — xem <see cref="SearchAsync"/> cho AC1, <see cref="GetReportAsync"/>
/// vẫn giữ NGUYÊN logic tính toán, chỉ đổi THỜI ĐIỂM/TẦN SUẤT gọi ở client — AC2/AC3/AC4).
/// </summary>
/// <remarks>
/// <b>Nguồn dòng báo cáo (AC2/AC14, viết lại LẦN 3 — 25/08/2026)</b>: với mỗi cặp (Line, Công đoạn "Đóng thùng" —
/// <see cref="Stage.IsPackingStage"/> = <c>true</c>) có ÍT NHẤT 1 <see cref="ProductionPlanStage"/> ở
/// <see cref="PlanStatus"/> KHÁC <see cref="PlanStatus.Draft"/>, chọn 1 bản ghi ĐẠI DIỆN theo thứ tự ưu tiên
/// <see cref="PlanStatus.Running"/> &gt; <see cref="PlanStatus.Paused"/> &gt; <see cref="PlanStatus.Completed"/>
/// &gt; <see cref="PlanStatus.Cancelled"/> (nhiều bản ghi cùng mức ưu tiên -> chọn Id lớn nhất) rồi sinh 1 dòng đại
/// diện — KHÔNG còn giới hạn chỉ Running như bản LẦN 2 (giải quyết gap "Lot Paused/Completed/Cancelled không tra
/// cứu được"). KHÔNG hiển thị dòng "chưa có kế hoạch" như <see cref="ProductionReportService"/> (US-21) — chỉ
/// liệt kê cặp (Line, Đóng thùng) đã từng "Áp dụng" ít nhất 1 lần (loại Draft), không cần placeholder.
///
/// <b>Gộp số thùng/số lượng theo Lot (Quyết định 24/08/2026, cùng tinh thần Quyết định 18/08/2026 ở
/// <see cref="ProductionReportService"/>/US-21)</b>: 1 Lot có thể có nhiều <see cref="ProductionPlan"/> tại CÙNG
/// (Line, Công đoạn Đóng thùng) nếu kế hoạch cũ bị <see cref="PlanStatus.Cancelled"/> rồi tạo lại — số thùng/số
/// lượng đã đóng của Lot đó không được "mất" theo kế hoạch cũ. Vì vậy, với dòng đang Running của (Line, Công đoạn),
/// tìm TẤT CẢ <see cref="ProductionPlanStage"/> khác cùng (Line, Công đoạn) đó mà kế hoạch tương ứng cùng
/// <see cref="ProductionPlan.Lot"/> (không giới hạn <see cref="PlanStatus"/>), rồi SUM <see cref="PackingBox"/>
/// (<see cref="PackingBoxStatus.Completed"/>) của TẤT CẢ <see cref="ProductionPlan.Id"/> đó — KHÔNG chỉ riêng kế
/// hoạch đang Running.
///
/// <b>% hoàn thành (AC2/AC3)</b>: đối chiếu <see cref="PackingProgressReportRowDto.PackedOkQuantity"/> với
/// <see cref="Domain.Entities.Lot.TotalQuantity"/> (nhập tay, US-21a) theo đúng mã Lot — <c>null</c> khi
/// <c>TotalQuantity</c> chưa từng được nhập ("Chưa xác định", AC3), dùng lại đúng quy ước Đủ/Chưa đủ đã có ở
/// <c>LotStageRowDto.IsSufficientQuantity</c> (US-21a AC5), KHÔNG tự chế nhãn/trạng thái "hoàn thành" nào khác.
///
/// <b>Edge case đã biết, CHƯA có AC yêu cầu xử lý riêng</b>: nếu 2 Line khác nhau CÙNG lúc có kế hoạch Running
/// SONG SONG cho CÙNG 1 Lot tại công đoạn Đóng thùng (hiếm — thường 1 Lot chạy trên 1 Line), mỗi Line vẫn ra 1
/// dòng riêng (đúng AC1/AC4 — "mỗi dòng ứng 1 ProductionPlanStage Running"), nhưng cả 2 dòng sẽ hiển thị CÙNG 1
/// số thùng/số lượng gộp (vì gộp theo Lot không phân biệt Line) — chấp nhận được vì SRS không có kịch bản 1 Lot
/// chạy đồng thời nhiều Line.
/// </remarks>
public class PackingProgressReportService : IPackingProgressReportService
{
    /// <summary>AC1: giới hạn số gợi ý autocomplete trả về, cùng giá trị/tinh thần <c>LotReportService.MaxSearchResults</c> (US-21).</summary>
    private const int MaxSearchResults = 20;

    private readonly IUnitOfWork _unitOfWork;

    public PackingProgressReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// AC1 (viết lại LẦN 2 — 25/08/2026): truy vấn NHẸ — CHỈ tìm trong các <see cref="ProductionPlanStage"/> đang
    /// <see cref="PlanStatus.Running"/> tại công đoạn Đóng thùng (<see cref="Stage.IsPackingStage"/>), KHÔNG tính
    /// SUM/gộp <see cref="PackingBox"/> như <see cref="GetReportAsync"/>. Gộp DUY NHẤT theo Lot (dedupe) — KHÔNG
    /// lặp lại nhiều lần cho cùng 1 Lot dù Lot đó đang chạy đồng thời nhiều Line (AC4); việc phân biệt theo Line dời
    /// xuống bảng kết quả + dropdown lọc Line (AC2), không lọc theo Line ở bước gợi ý này.
    /// </summary>
    public async Task<IReadOnlyList<PackingProgressSearchItemDto>> SearchAsync(string? search, CancellationToken cancellationToken = default)
    {
        var trimmed = search?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Array.Empty<PackingProgressSearchItemDto>();
        }

        var packingStages = await _unitOfWork.Repository<Stage>().FindAsync(s => s.IsPackingStage, cancellationToken);
        var packingStageIds = packingStages.Select(s => s.Id).ToHashSet();
        if (packingStageIds.Count == 0)
        {
            return Array.Empty<PackingProgressSearchItemDto>();
        }

        // AC1 (viết lại LẦN 3 — 25/08/2026): ProductionPlanStage tại công đoạn Đóng thùng ở PlanStatus KHÁC Draft
        // (Running/Paused/Completed/Cancelled) — loại Draft vì kế hoạch chưa từng "Áp dụng" chắc chắn chưa có dữ
        // liệu đóng thùng nào. Không cần đủ lịch sử để gộp SUM ở bước này (khác GetReportAsync).
        var eligiblePlanStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            ps => packingStageIds.Contains(ps.StageId) && ps.PlanStatus != PlanStatus.Draft, cancellationToken);
        if (eligiblePlanStages.Count == 0)
        {
            return Array.Empty<PackingProgressSearchItemDto>();
        }

        var planIds = eligiblePlanStages.Select(ps => ps.ProductionPlanId).Distinct().ToList();
        var plansById = (await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => planIds.Contains(p.Id), cancellationToken))
            .ToDictionary(p => p.Id);

        return eligiblePlanStages
            .Where(ps => plansById.TryGetValue(ps.ProductionPlanId, out var p) && p.Lot.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(ps => plansById[ps.ProductionPlanId].Lot)
            .Distinct()
            .OrderBy(lot => lot, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSearchResults)
            .Select(lot => new PackingProgressSearchItemDto { Lot = lot })
            .ToList();
    }

    /// <summary>
    /// AC14 (25/08/2026): thứ tự ưu tiên chọn bản ghi đại diện khi 1 cặp (Line, Đóng thùng) có nhiều
    /// <see cref="ProductionPlanStage"/> lịch sử cùng Lot — <c>Running</c> &gt; <c>Paused</c> &gt; <c>Completed</c>
    /// &gt; <c>Cancelled</c> (loại <c>Draft</c> — không đại diện được, đã lọc trước khi gọi).
    /// </summary>
    private static int GetPlanStatusPriority(PlanStatus status) => status switch
    {
        PlanStatus.Running => 0,
        PlanStatus.Paused => 1,
        PlanStatus.Completed => 2,
        PlanStatus.Cancelled => 3,
        _ => int.MaxValue,
    };

    public async Task<PackingProgressReportDto> GetReportAsync(PackingProgressReportQuery query, CancellationToken cancellationToken = default)
    {
        var packingStages = await _unitOfWork.Repository<Stage>().FindAsync(s => s.IsPackingStage, cancellationToken);
        var packingStageIds = packingStages.Select(s => s.Id).ToHashSet();
        if (packingStageIds.Count == 0)
        {
            return new PackingProgressReportDto { GeneratedAtUtc = DateTime.UtcNow, Rows = Array.Empty<PackingProgressReportRowDto>() };
        }

        // AC2/gộp theo Lot: TẤT CẢ ProductionPlanStage đã từng cấu hình tại công đoạn Đóng thùng, KHÔNG giới hạn
        // PlanStatus — cần đủ lịch sử để gộp (SUM) đúng khi kế hoạch cũ Cancelled rồi tạo lại (xem remarks).
        var planStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            ps => packingStageIds.Contains(ps.StageId) && (query.LineId == null || ps.LineId == query.LineId),
            cancellationToken);

        if (planStages.Count == 0)
        {
            return new PackingProgressReportDto { GeneratedAtUtc = DateTime.UtcNow, Rows = Array.Empty<PackingProgressReportRowDto>() };
        }

        var planIds = planStages.Select(ps => ps.ProductionPlanId).Distinct().ToList();
        var plansById = (await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => planIds.Contains(p.Id), cancellationToken))
            .ToDictionary(p => p.Id);

        var lineIds = planStages.Select(ps => ps.LineId).Distinct().ToList();
        var linesById = (await _unitOfWork.Repository<Line>().FindAsync(l => lineIds.Contains(l.Id), cancellationToken))
            .ToDictionary(l => l.Id);

        var stagesById = packingStages.ToDictionary(s => s.Id);

        // Chỉ cần thùng Completed (AC2 "số lượng sản phẩm đã đóng thùng OK" — KHÔNG tính thùng InProgress dở).
        var completedBoxes = await _unitOfWork.Repository<PackingBox>().FindAsync(
            b => planIds.Contains(b.ProductionPlanId) && packingStageIds.Contains(b.StageId) && b.Status == PackingBoxStatus.Completed,
            cancellationToken);

        var pairs = planStages.Select(ps => (ps.LineId, ps.StageId)).Distinct().ToList();

        var rows = new List<PackingProgressReportRowDto>();
        foreach (var (lineId, stageId) in pairs)
        {
            var pairPlanStages = planStages.Where(ps => ps.LineId == lineId && ps.StageId == stageId).ToList();

            // AC2/AC14 (viết lại LẦN 3 — 25/08/2026): chọn bản ghi ĐẠI DIỆN cho cặp (Line, Đóng thùng) theo thứ tự
            // ưu tiên PlanStatus Running > Paused > Completed > Cancelled (loại Draft — kế hoạch chưa từng "Áp
            // dụng" chắc chắn chưa có dữ liệu đóng thùng). Nhiều bản ghi cùng mức ưu tiên -> chọn Id lớn nhất (gần
            // nhất, entity không có field thời gian tạo).
            var representativePlanStage = pairPlanStages
                .Where(ps => ps.PlanStatus != PlanStatus.Draft)
                .OrderBy(ps => GetPlanStatusPriority(ps.PlanStatus))
                .ThenByDescending(ps => ps.Id)
                .FirstOrDefault();
            if (representativePlanStage is null)
            {
                continue;
            }

            var representativePlan = plansById[representativePlanStage.ProductionPlanId];

            // AC4: lọc Lot/Model theo kế hoạch đại diện (loại hẳn dòng không khớp, không hiển thị dòng nhiễu).
            if (query.Lot != null && representativePlan.Lot != query.Lot)
            {
                continue;
            }

            if (query.Model != null && representativePlan.Model != query.Model)
            {
                continue;
            }

            // Gộp (SUM) theo Lot — xem remarks: mọi ProductionPlan khác (mọi PlanStatus) cùng (Line, Đóng thùng)
            // này mà cùng Lot với kế hoạch đại diện — KHÔNG đổi dù bản ghi đại diện không còn giới hạn Running.
            var lotGroupPlanIds = pairPlanStages
                .Select(ps => ps.ProductionPlanId)
                .Where(id => plansById.TryGetValue(id, out var p) && p.Lot == representativePlan.Lot)
                .Distinct()
                .ToHashSet();

            var boxesForLot = completedBoxes.Where(b => b.StageId == stageId && lotGroupPlanIds.Contains(b.ProductionPlanId)).ToList();
            var completedBoxCount = boxesForLot.Count;
            var packedOkQuantity = boxesForLot.Sum(b => b.ScannedQuantity);

            rows.Add(new PackingProgressReportRowDto
            {
                ProductionPlanId = representativePlan.Id,
                LineId = lineId,
                LineName = linesById.TryGetValue(lineId, out var line) ? line.Name : $"#{lineId}",
                StageId = stageId,
                StageName = stagesById.TryGetValue(stageId, out var stage) ? stage.Name : $"#{stageId}",
                Model = representativePlan.Model,
                Lot = representativePlan.Lot,
                PlanStatus = representativePlanStage.PlanStatus,
                CompletedBoxCount = completedBoxCount,
                PackedOkQuantity = packedOkQuantity,
            });
        }

        // AC2/AC3: đối chiếu "Tổng số lượng Lot" (US-21a) theo đúng mã Lot của TỪNG dòng — 1 lượt tra cứu cho
        // toàn bộ Lot xuất hiện trong rows, tránh N+1 query.
        var lotCodes = rows.Select(r => r.Lot).Distinct().ToList();
        var lotsByCode = lotCodes.Count == 0
            ? new Dictionary<string, Lot>()
            : (await _unitOfWork.Repository<Lot>().FindAsync(l => lotCodes.Contains(l.Code), cancellationToken))
                .ToDictionary(l => l.Code);

        foreach (var row in rows)
        {
            var totalQuantity = lotsByCode.TryGetValue(row.Lot, out var lotEntity) ? lotEntity.TotalQuantity : null;
            row.LotTotalQuantity = totalQuantity;

            if (totalQuantity is null)
            {
                // AC3: "Chưa xác định" — không suy diễn 0%.
                continue;
            }

            row.IsSufficientQuantity = row.PackedOkQuantity >= totalQuantity.Value;
            // AC2: % hoàn thành, làm tròn 2 chữ số thập phân. Guard chia 0 khi "Tổng số lượng Lot" nhập = 0 (edge
            // hiếm, không có AC yêu cầu chặn nhập 0 ở US-21a) — coi như đã đủ (>=100%) nếu đã đóng >=1 sản phẩm.
            row.CompletionPercentage = totalQuantity.Value > 0
                ? Math.Round((decimal)row.PackedOkQuantity / totalQuantity.Value * 100m, 2)
                : (row.PackedOkQuantity > 0 ? 100m : 0m);
        }

        return new PackingProgressReportDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Rows = rows.OrderBy(r => r.LineName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Lot, StringComparer.OrdinalIgnoreCase).ToList(),
        };
    }

    /// <summary>
    /// AC6: dùng lại ĐÚNG cách gộp lotGroupPlanIds ở <see cref="GetReportAsync"/> — TẤT CẢ <see cref="ProductionPlanStage"/>
    /// cùng (<paramref name="lineId"/>, Công đoạn Đóng thùng) mà <see cref="ProductionPlan.Lot"/> trùng
    /// <paramref name="lot"/> (không giới hạn <see cref="PlanStatus"/>), rồi lấy TẤT CẢ <see cref="PackingBox"/>
    /// (Completed lẫn InProgress, khác <see cref="GetReportAsync"/> chỉ lấy Completed) của các kế hoạch đó.
    /// </summary>
    public async Task<IReadOnlyList<PackingProgressReportBoxDto>> GetBoxesAsync(int lineId, string lot, CancellationToken cancellationToken = default)
    {
        var packingStages = await _unitOfWork.Repository<Stage>().FindAsync(s => s.IsPackingStage, cancellationToken);
        var packingStageIds = packingStages.Select(s => s.Id).ToHashSet();
        if (packingStageIds.Count == 0)
        {
            return Array.Empty<PackingProgressReportBoxDto>();
        }

        // TẤT CẢ ProductionPlanStage đã từng cấu hình tại (Line, Đóng thùng), KHÔNG giới hạn PlanStatus (AC6 —
        // "kể cả kế hoạch cũ đã Cancelled"), cùng nguồn dữ liệu với GetReportAsync.
        var planStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            ps => ps.LineId == lineId && packingStageIds.Contains(ps.StageId), cancellationToken);
        if (planStages.Count == 0)
        {
            return Array.Empty<PackingProgressReportBoxDto>();
        }

        var planIds = planStages.Select(ps => ps.ProductionPlanId).Distinct().ToList();
        var plansById = (await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => planIds.Contains(p.Id), cancellationToken))
            .ToDictionary(p => p.Id);

        var lotGroupPlanIds = planStages
            .Select(ps => ps.ProductionPlanId)
            .Where(id => plansById.TryGetValue(id, out var p) && p.Lot == lot)
            .Distinct()
            .ToHashSet();

        if (lotGroupPlanIds.Count == 0)
        {
            return Array.Empty<PackingProgressReportBoxDto>();
        }

        // AC6: CẢ Completed lẫn InProgress — khác GetReportAsync (chỉ Completed).
        var boxes = await _unitOfWork.Repository<PackingBox>().FindAsync(
            b => lotGroupPlanIds.Contains(b.ProductionPlanId) && packingStageIds.Contains(b.StageId), cancellationToken);

        return boxes
            .OrderBy(b => b.BoxNo)
            .Select(b => new PackingProgressReportBoxDto
            {
                Id = b.Id,
                BoxNo = b.BoxNo,
                Status = b.Status,
                ScannedQuantity = b.ScannedQuantity,
                TargetQuantity = b.TargetQuantity,
                OpenedAtUtc = b.OpenedAtUtc,
                CompletedAtUtc = b.CompletedAtUtc,
            })
            .ToList();
    }

    /// <summary>
    /// AC7/AC8: chỉ lấy <see cref="Scan"/> có <see cref="Scan.PackingBoxId"/> = <paramref name="packingBoxId"/> VÀ
    /// <see cref="Scan.Result"/> = <see cref="ScanResult.Ok"/> (KHÔNG hiển thị lượt bị từ chối). Xem
    /// <see cref="PackingProgressReportBoxScansDto.HasDetailedScanData"/> để biết cách phân biệt "0 lượt thật" với
    /// "không có dữ liệu chi tiết vì thùng cũ" (AC8).
    /// </summary>
    public async Task<PackingProgressReportBoxScansDto> GetBoxScansAsync(int packingBoxId, CancellationToken cancellationToken = default)
    {
        var box = await _unitOfWork.Repository<PackingBox>().GetByIdAsync(packingBoxId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy thùng với Id = {packingBoxId}.");

        var scans = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.PackingBoxId == packingBoxId && s.Result == ScanResult.Ok, cancellationToken);

        var ordered = scans.OrderBy(s => s.ScannedAtUtc).ThenBy(s => s.Id).ToList();

        // AC8: thùng thật sự chưa có scan nào (ScannedQuantity = 0, vd vừa mở) vẫn coi là "có dữ liệu chi tiết"
        // (đúng là 0 lượt) — CHỈ false khi ScannedQuantity > 0 mà không tìm thấy Scan nào gắn PackingBoxId (dữ
        // liệu cũ ghi trước khi field này tồn tại, không backfill).
        var hasDetailedScanData = box.ScannedQuantity == 0 || ordered.Count > 0;

        return new PackingProgressReportBoxScansDto
        {
            HasDetailedScanData = hasDetailedScanData,
            Scans = ordered.Select(s => new PackingProgressReportBoxScanDto { TagCode = s.TagCode, ScannedAtUtc = s.ScannedAtUtc }).ToList(),
        };
    }
}
