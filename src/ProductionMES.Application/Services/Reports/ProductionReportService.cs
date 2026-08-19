using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.AndonBoard;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// Implementation IProductionReportService (US-21/FR-21 — mở rộng 18/08/2026, AC4-AC8). Tái sử dụng NGUYÊN VẸN
/// phần tính toán thuần túy <see cref="AndonBoardCalculator"/> (đã kiểm thử đầy đủ ở US-09) cho công thức "PLAN
/// lũy kế đã trừ khung giờ nghỉ" — không sửa <see cref="AndonBoardCalculator"/>, chỉ gọi lại với 2 mốc thời gian
/// khác nhau rồi lấy hiệu số để ra PLAN trong 1 khoảng (xem <see cref="BuildRow"/>).
/// </summary>
/// <remarks>
/// <b>Thiết kế nhóm dòng báo cáo (AC4, Quyết định 18/08/2026)</b>: mỗi dòng báo cáo đại diện 1 bộ (Line, Công
/// đoạn, Lot) — KHÔNG còn (Line, Công đoạn) như bản gốc AC1-AC3. Tính theo 2 bước:
/// 1. Với mỗi cặp (Line, Công đoạn) đang có trạm làm việc hoạt động (<see cref="WorkStation.IsActive"/> = true),
///    tìm TẤT CẢ <see cref="ProductionPlanStage"/> đã từng cấu hình cho cặp đó (không giới hạn <c>PlanStatus</c>),
///    khớp thêm bộ lọc Model/Customer/Revision/Lot (AC6) nếu có truyền.
/// 2. GỘP (SUM) các kế hoạch có cùng <see cref="ProductionPlan.Lot"/> thành 1 dòng — cộng dồn ACTUAL (Ok)/NG (Ng)/
///    PLAN cumulative của TỪNG kế hoạch (tính riêng theo đúng <c>StartTime</c> + khung giờ nghỉ của kế hoạch đó,
///    rồi mới cộng) — xử lý đúng trường hợp 1 Lot bị tạo lại thành kế hoạch mới sau khi kế hoạch cũ bị
///    <c>Cancelled</c> (Quyết định đã chốt với người giao việc, xem BACKLOG-user-story.md mục US-21).
///
/// <b>Dòng nào được hiển thị (rows)</b>:
/// - AC1 (không truyền FromUtc/ToUtc, "thời gian thực"): chỉ hiển thị dòng cho cặp (Line, Công đoạn) đang có 1
///   <see cref="ProductionPlanStage"/> ở trạng thái <see cref="PlanStatus.Running"/> khớp bộ lọc — giữ nguyên hành
///   vi AC1 gốc (tránh liệt kê lại toàn bộ lịch sử Lot đã Completed từ rất lâu trên bảng real-time). Lot của dòng
///   = Lot của kế hoạch đang Running đó; sau đó GỘP thêm mọi kế hoạch khác (đã Cancelled/Completed) cùng
///   (Line, Công đoạn, Lot) này theo đúng Quyết định ở trên.
/// - AC2 (có truyền FromUtc và/hoặc ToUtc, "khoảng thời gian đã qua"): hiển thị 1 dòng cho MỌI Lot khác nhau từng
///   xuất hiện ở (Line, Công đoạn) đó (không giới hạn PlanStatus — khoảng quá khứ có thể rơi vào kế hoạch đã
///   Completed/Cancelled), khớp bộ lọc.
/// - Khi đã áp dụng ít nhất 1 bộ lọc Model/Customer/Revision/Lot (AC6) mà 1 cặp (Line, Công đoạn) không còn kế
///   hoạch nào khớp, cặp đó bị LOẠI HẲN khỏi kết quả (không hiển thị dòng "Chưa có kế hoạch" gây nhiễu) — khác
///   trường hợp KHÔNG lọc gì, khi đó vẫn hiển thị dòng <c>HasPlanData</c> = false cho cặp chưa từng có kế hoạch
///   (giữ nguyên hành vi AC1-AC3 gốc).
///
/// AC3/AC5: ACTUAL chỉ đếm <see cref="ScanResult.Ok"/>, NG chỉ đếm <see cref="ScanResult.Ng"/> — không có khái
/// niệm "Chờ đồng bộ" ở tầng server (mọi bản ghi đã INSERT vào bảng Scan đều coi là đã xác nhận).
/// </remarks>
public class ProductionReportService : IProductionReportService
{
    private const decimal SecondsPerHour = 3600m;

    private readonly IUnitOfWork _unitOfWork;

    public ProductionReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductionReportDto> GetReportAsync(ProductionReportQuery query, CancellationToken cancellationToken = default)
    {
        var isRangeMode = query.FromUtc is not null || query.ToUtc is not null;
        var atUtc = query.ToUtc ?? DateTime.UtcNow;
        var atLocal = atUtc.ToLocalTime();
        var fromLocal = query.FromUtc?.ToLocalTime();
        var hasAnyPlanFilter = query.Model != null || query.Customer != null || query.Revision != null || query.Lot != null;

        var workStations = await _unitOfWork.Repository<WorkStation>().FindAsync(
            w => w.IsActive
                 && (query.LineId == null || w.LineId == query.LineId)
                 && (query.StageId == null || w.StageId == query.StageId),
            cancellationToken);

        var pairs = workStations
            .Select(w => (w.LineId, w.StageId))
            .Distinct()
            .OrderBy(p => p.LineId).ThenBy(p => p.StageId)
            .ToList();

        if (pairs.Count == 0)
        {
            return new ProductionReportDto { FromUtc = query.FromUtc, ToUtc = query.ToUtc, GeneratedAtUtc = DateTime.UtcNow, Rows = Array.Empty<ProductionReportRowDto>() };
        }

        var lineIds = pairs.Select(p => p.LineId).Distinct().ToList();
        var stageIds = pairs.Select(p => p.StageId).Distinct().ToList();

        var lines = await _unitOfWork.Repository<Line>().FindAsync(l => lineIds.Contains(l.Id), cancellationToken);
        var stages = await _unitOfWork.Repository<Stage>().FindAsync(s => stageIds.Contains(s.Id), cancellationToken);
        var breakWindows = await _unitOfWork.Repository<BreakWindow>().FindAsync(b => lineIds.Contains(b.LineId), cancellationToken);

        // TẤT CẢ ProductionPlanStage đã từng cấu hình cho các cặp (Line, Công đoạn) này, KHÔNG giới hạn PlanStatus
        // — cần đầy đủ lịch sử để gộp (SUM) đúng theo Lot (khác bản gốc AC1-AC3 chỉ cần 1 kế hoạch tham chiếu).
        var planStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            ps => lineIds.Contains(ps.LineId) && stageIds.Contains(ps.StageId), cancellationToken);

        var planIds = planStages.Select(ps => ps.ProductionPlanId).Distinct().ToList();
        var plans = planIds.Count == 0
            ? new List<ProductionPlan>()
            : (await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => planIds.Contains(p.Id), cancellationToken)).ToList();

        // AC6: lọc Model/Customer/Revision/Lot ở cấp kế hoạch (so khớp tuyệt đối) — dữ liệu này bị khóa tuyệt đối
        // sau lượt scan đầu tiên (mục 6 quy tắc 14 SRS) nên tương đương snapshot trên Scan.
        var filteredPlanIds = plans
            .Where(p =>
                (query.Model == null || p.Model == query.Model) &&
                (query.Customer == null || p.Customer == query.Customer) &&
                (query.Revision == null || p.Revision == query.Revision) &&
                (query.Lot == null || p.Lot == query.Lot))
            .Select(p => p.Id)
            .ToHashSet();
        var plansById = plans.ToDictionary(p => p.Id);

        // AC3/AC5: chỉ Ok (ACTUAL) và Ng (NgCount) mới có thể lọt vào báo cáo — DuplicateTag/PreviousStageNotPassed/
        // WaitingReworkUnlock không thuộc phạm vi báo cáo tổng hợp.
        var relevantScans = (await _unitOfWork.Repository<Scan>().FindAsync(
            s => (s.Result == ScanResult.Ok || s.Result == ScanResult.Ng) && lineIds.Contains(s.LineId) && stageIds.Contains(s.StageId),
            cancellationToken)).ToList();

        var rows = new List<ProductionReportRowDto>();

        foreach (var (lineId, stageId) in pairs)
        {
            var lineName = lines.FirstOrDefault(l => l.Id == lineId)?.Name ?? $"#{lineId}";
            var stageName = stages.FirstOrDefault(s => s.Id == stageId)?.Name ?? $"#{stageId}";
            var lineBreakWindows = breakWindows.Where(b => b.LineId == lineId).Select(b => (b.StartTime, b.EndTime)).ToList();

            // Chỉ giữ lại các ProductionPlanStage của cặp này mà kế hoạch tương ứng khớp bộ lọc AC6 (nếu có).
            var pairPlanStages = planStages
                .Where(ps => ps.LineId == lineId && ps.StageId == stageId && filteredPlanIds.Contains(ps.ProductionPlanId))
                .ToList();

            if (!isRangeMode)
            {
                // AC1: chỉ hiển thị dòng khi có kế hoạch đang Running (giữ nguyên hành vi AC1 gốc — tránh liệt kê
                // lại toàn bộ Lot lịch sử đã đóng từ lâu trên bảng thời gian thực).
                var runningPlanStage = pairPlanStages.FirstOrDefault(ps => ps.PlanStatus == PlanStatus.Running);
                if (runningPlanStage is null)
                {
                    if (hasAnyPlanFilter)
                    {
                        continue; // Không còn gì khớp bộ lọc đang áp dụng cho cặp này -> loại hẳn, không hiển thị dòng nhiễu.
                    }

                    rows.Add(BuildNoPlanRow(lineId, lineName, stageId, stageName));
                    continue;
                }

                var runningPlan = plansById[runningPlanStage.ProductionPlanId];
                var lotGroupPlans = pairPlanStages
                    .Select(ps => plansById[ps.ProductionPlanId])
                    .Where(p => p.Lot == runningPlan.Lot)
                    .DistinctBy(p => p.Id)
                    .ToList();

                rows.Add(BuildRow(
                    lineId, lineName, stageId, stageName, runningPlan.Lot, lotGroupPlans,
                    lineBreakWindows, fromLocal: null, atLocal, query, atUtc, relevantScans));
            }
            else
            {
                if (pairPlanStages.Count == 0)
                {
                    if (hasAnyPlanFilter)
                    {
                        continue;
                    }

                    rows.Add(BuildNoPlanRow(lineId, lineName, stageId, stageName));
                    continue;
                }

                var lotGroups = pairPlanStages
                    .Select(ps => plansById[ps.ProductionPlanId])
                    .DistinctBy(p => p.Id)
                    .GroupBy(p => p.Lot)
                    .OrderBy(g => g.Key);

                foreach (var lotGroup in lotGroups)
                {
                    rows.Add(BuildRow(
                        lineId, lineName, stageId, stageName, lotGroup.Key, lotGroup.ToList(),
                        lineBreakWindows, fromLocal, atLocal, query, atUtc, relevantScans));
                }
            }
        }

        return new ProductionReportDto
        {
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
            GeneratedAtUtc = DateTime.UtcNow,
            Rows = rows,
        };
    }

    /// <summary>
    /// AC4/AC5: gộp (SUM) ACTUAL/NG/PLAN của TẤT CẢ kế hoạch trong <paramref name="plans"/> (cùng Line, Công đoạn,
    /// Lot) thành 1 dòng — mỗi kế hoạch tính PLAN cumulative riêng theo đúng <c>StartTime</c> của nó rồi mới cộng
    /// (Quyết định 18/08/2026).
    /// </summary>
    private static ProductionReportRowDto BuildRow(
        int lineId, string lineName, int stageId, string stageName, string lot, IReadOnlyList<ProductionPlan> plans,
        IReadOnlyList<(TimeOnly Start, TimeOnly End)> lineBreakWindows, DateTime? fromLocal, DateTime atLocal,
        ProductionReportQuery query, DateTime atUtc, IReadOnlyList<Scan> relevantScans)
    {
        var actual = 0;
        var ngCount = 0;
        var plan = 0;
        var planIds = new List<int>();

        foreach (var productionPlan in plans)
        {
            planIds.Add(productionPlan.Id);

            var standardQuantityPerHourExact = productionPlan.TaktTimeSeconds > 0 ? SecondsPerHour / productionPlan.TaktTimeSeconds : 0m;
            var planAtTo = AndonBoardCalculator.ComputePlanCumulative(standardQuantityPerHourExact, productionPlan.StartTime, atLocal, lineBreakWindows);
            var planAtFrom = fromLocal is null
                ? 0
                : AndonBoardCalculator.ComputePlanCumulative(standardQuantityPerHourExact, productionPlan.StartTime, fromLocal.Value, lineBreakWindows);
            plan += planAtTo - planAtFrom;

            actual += relevantScans.Count(s =>
                s.ProductionPlanId == productionPlan.Id && s.StageId == stageId && s.Result == ScanResult.Ok &&
                (query.FromUtc == null || s.ScannedAtUtc >= query.FromUtc) && s.ScannedAtUtc <= atUtc);

            ngCount += relevantScans.Count(s =>
                s.ProductionPlanId == productionPlan.Id && s.StageId == stageId && s.Result == ScanResult.Ng &&
                (query.FromUtc == null || s.ScannedAtUtc >= query.FromUtc) && s.ScannedAtUtc <= atUtc);
        }

        return new ProductionReportRowDto
        {
            LineId = lineId,
            LineName = lineName,
            StageId = stageId,
            StageName = stageName,
            Lot = lot,
            HasPlanData = true,
            ProductionPlanIds = planIds,
            Actual = actual,
            NgCount = ngCount,
            Plan = plan,
            Balance = actual - plan,
        };
    }

    private static ProductionReportRowDto BuildNoPlanRow(int lineId, string lineName, int stageId, string stageName) => new()
    {
        LineId = lineId,
        LineName = lineName,
        StageId = stageId,
        StageName = stageName,
        Lot = string.Empty,
        HasPlanData = false,
    };
}
