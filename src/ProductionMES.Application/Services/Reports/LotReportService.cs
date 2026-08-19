using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// Implementation <see cref="ILotReportService"/> (US-21/FR-21, vòng 3 — 18/08/2026, AC1-AC5; US-21a AC5/AC6,
/// viết lại hoàn toàn 19/08/2026 — "Tổng số lượng Lot" đọc từ entity <see cref="Lot"/> nhập tay, KHÔNG còn tính
/// SUM(PlannedQuantity) như bản đề xuất ban đầu).
/// </summary>
/// <remarks>
/// <b>AC4 — xác định danh sách (Line, Công đoạn) của 1 Lot</b>: hợp (union) 2 nguồn — (a) <c>LineId</c>/<c>StageId</c>
/// của mọi <see cref="ProductionPlanStage"/> thuộc các <see cref="ProductionPlan"/> cùng Lot (không giới hạn
/// <c>PlanStatus</c>), và (b) <c>LineId</c>/<c>StageId</c> snapshot trực tiếp trên <see cref="Scan"/> có cùng
/// <c>Lot</c> (US-10 AC4) — nguồn (b) là lưới an toàn phòng trường hợp cấu hình <see cref="ProductionPlanStage"/>
/// đã bị gỡ sau khi từng có lượt scan (AC4 yêu cầu "đã từng có ít nhất 1 ProductionPlanStage cấu hình HOẶC ít
/// nhất 1 lượt scan").
///
/// <b>AC5</b>: OK/NG mỗi dòng đếm trực tiếp trên <see cref="Scan"/> (snapshot <c>Lot</c>, US-10 AC4) theo khoảng
/// [FromUtc, ToUtc] tùy chọn — KHÔNG dùng <c>AndonBoardCalculator</c>/gộp theo <c>ProductionPlanId</c> như
/// <see cref="ProductionReportService"/> (bản vòng 2), vì AC6 (vòng 3) không yêu cầu PLAN/BALANCE nên không cần
/// biết "kế hoạch tham chiếu" của từng lượt scan, chỉ cần đúng Lot + Line + Công đoạn + khoảng thời gian.
///
/// <b>US-21a AC5/AC6</b>: "Tổng số lượng Lot" tra trực tiếp entity <see cref="Lot"/> theo <c>Code = lot</c> qua
/// <c>IUnitOfWork</c> (KHÔNG qua <c>Services.Lots.ILotService</c> — tránh phụ thuộc vòng, vì <c>LotService</c>
/// đọc lại chính <see cref="GetLotSummaryAsync"/> này để tính soft-confirm AC3/AC8 US-05). <c>null</c> khi chưa
/// từng có ai nhập (AC6 "Chưa xác định") — mỗi dòng breakdown (<see cref="LotStageRowDto.IsSufficientQuantity"/>)
/// so sánh riêng OkCount với giá trị này, KHÔNG gộp 1 số cho cả Lot (đã chốt với Ban quản lý).
/// </remarks>
public class LotReportService : ILotReportService
{
    /// <summary>AC1: giới hạn số gợi ý autocomplete trả về, tránh danh sách quá dài khi search khớp nhiều Lot.</summary>
    private const int MaxSearchResults = 20;

    private readonly IUnitOfWork _unitOfWork;

    public LotReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<LotSearchItemDto>> SearchLotsAsync(string? search, CancellationToken cancellationToken = default)
    {
        var trimmed = search?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Array.Empty<LotSearchItemDto>();
        }

        // Không có entity Lot riêng cho việc TÌM KIẾM (chỉ dùng entity Lot cho "Tổng số lượng Lot") — vẫn quét
        // DISTINCT ProductionPlan.Lot khớp gần đúng như trước, vì đây là autocomplete theo lịch sử kế hoạch.
        var plans = await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => p.Lot.Contains(trimmed), cancellationToken);

        return plans
            .Select(p => p.Lot)
            .Distinct()
            .OrderBy(lot => lot, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSearchResults)
            .Select(lot => new LotSearchItemDto { Lot = lot })
            .ToList();
    }

    public async Task<LotSummaryDto?> GetLotSummaryAsync(string lot, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var plans = (await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => p.Lot == lot, cancellationToken)).ToList();
        if (plans.Count == 0)
        {
            // AC2: "Không tìm thấy Lot" — Controller quy đổi thành 404, KHÔNG phải lỗi hệ thống.
            return null;
        }

        // AC3: đồng nhất hay không, hiển thị TẤT CẢ giá trị khác nhau tìm được — không tự chọn 1 giá trị đại diện.
        var models = plans.Select(p => p.Model).Distinct().OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
        var customers = plans.Select(p => p.Customer).Distinct().OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        var revisions = plans.Select(p => p.Revision ?? string.Empty).Distinct().OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList();

        // US-21a AC1/AC5: "Tổng số lượng Lot" = giá trị NHẬP TAY trên entity Lot (Code = lot), KHÔNG phải SUM.
        var lotEntity = (await _unitOfWork.Repository<Lot>().FindAsync(l => l.Code == lot, cancellationToken)).FirstOrDefault();
        var lotTotalQuantity = lotEntity?.TotalQuantity;

        var planIds = plans.Select(p => p.Id).ToHashSet();
        var planStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(ps => planIds.Contains(ps.ProductionPlanId), cancellationToken);

        // AC5: chỉ Ok/Ng mới có ý nghĩa thống kê OK/NG (cùng quy tắc đã chốt US-09/US-18/US-21 vòng 2) — snapshot
        // trực tiếp trên Scan.Lot (US-10 AC4), không tra cứu động qua ProductionPlan hiện tại.
        var scans = (await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.Lot == lot && (s.Result == ScanResult.Ok || s.Result == ScanResult.Ng) &&
                 (fromUtc == null || s.ScannedAtUtc >= fromUtc) && (toUtc == null || s.ScannedAtUtc <= toUtc),
            cancellationToken)).ToList();

        // AC4: hợp (union) 2 nguồn (Line, Công đoạn) — xem remarks ở đầu file.
        var pairs = planStages.Select(ps => (ps.LineId, ps.StageId))
            .Concat(scans.Select(s => (s.LineId, s.StageId)))
            .Distinct()
            .OrderBy(p => p.LineId).ThenBy(p => p.StageId)
            .ToList();

        if (pairs.Count == 0)
        {
            return new LotSummaryDto
            {
                Lot = lot, Models = models, Customers = customers, Revisions = revisions, FromUtc = fromUtc, ToUtc = toUtc,
                Rows = Array.Empty<LotStageRowDto>(),
                LotTotalQuantity = lotTotalQuantity,
            };
        }

        var lineIds = pairs.Select(p => p.LineId).Distinct().ToList();
        var lines = (await _unitOfWork.Repository<Line>().FindAsync(l => lineIds.Contains(l.Id), cancellationToken)).ToList();

        var stageIds = pairs.Select(p => p.StageId).Distinct().ToList();
        var stages = await _unitOfWork.Repository<Stage>().FindAsync(s => stageIds.Contains(s.Id), cancellationToken);

        var rows = pairs.Select(p =>
        {
            var okCount = scans.Count(s => s.LineId == p.LineId && s.StageId == p.StageId && s.Result == ScanResult.Ok);
            var ngCount = scans.Count(s => s.LineId == p.LineId && s.StageId == p.StageId && s.Result == ScanResult.Ng);
            return new LotStageRowDto
            {
                LineId = p.LineId,
                LineName = lines.FirstOrDefault(l => l.Id == p.LineId)?.Name ?? $"#{p.LineId}",
                StageId = p.StageId,
                StageName = stages.FirstOrDefault(s => s.Id == p.StageId)?.Name ?? $"#{p.StageId}",
                OkCount = okCount,
                NgCount = ngCount,
                // US-21a AC5: so sánh THEO TỪNG DÒNG riêng biệt, null khi LotTotalQuantity chưa xác định (AC6).
                IsSufficientQuantity = lotTotalQuantity.HasValue ? okCount >= lotTotalQuantity.Value : null,
            };
        }).ToList();

        return new LotSummaryDto
        {
            Lot = lot, Models = models, Customers = customers, Revisions = revisions, FromUtc = fromUtc, ToUtc = toUtc, Rows = rows,
            LotTotalQuantity = lotTotalQuantity,
        };
    }
}
