using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.AndonBoard;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.AndonBoard;

/// <summary>
/// Implementation IAndonBoardService (US-09/FR-09/FR-09a). Tái sử dụng đúng cách xác định "kế hoạch active của
/// trạm" đã chốt ở US-05a/<c>ScanService</c>: cặp (Line, Công đoạn) của trạm đang có 1 <see cref="ProductionPlanStage"/>
/// ở trạng thái <see cref="PlanStatus.Running"/>. Toàn bộ phép tính PLAN/ACTUAL/BALANCE theo mốc giờ (bao gồm
/// trừ khung giờ nghỉ, AC5/AC6) giao cho <see cref="AndonBoardCalculator"/> (thuần túy, dễ test) — service này
/// chỉ chịu trách nhiệm nạp dữ liệu (WorkStation/BreakWindow/ProductionPlan/Scan qua Repository) và lắp ráp DTO.
/// </summary>
/// <remarks>
/// "ACTUAL"/"NG" tra cứu theo (ProductionPlanId, StageId) — giống hệt <c>ProductionPlanStageService.GetRunCountAsync</c>
/// (US-05a AC4), KHÔNG lọc thêm theo WorkStationId, vì 1 cặp (Kế hoạch, Công đoạn) chỉ có ý nghĩa 1 tiến độ duy
/// nhất bất kể trạm vật lý nào thực hiện (dù thực tế hiện tại mỗi Line+Stage thường chỉ có 1 WorkStation).
/// </remarks>
public class AndonBoardService : IAndonBoardService
{
    private const decimal SecondsPerHour = 3600m;

    /// <summary>Giới hạn số dòng mốc giờ cố định hiển thị (AC6) — phòng vệ StartTime bất thường (xem AndonBoardCalculator.BuildPastHourMarks).</summary>
    private const int MaxHourRows = 24;

    private readonly IUnitOfWork _unitOfWork;

    public AndonBoardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AndonBoardDto> GetForWorkStationAsync(int workStationId, CancellationToken cancellationToken = default)
    {
        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");

        var runningPlanStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            x => x.LineId == workStation.LineId && x.StageId == workStation.StageId && x.PlanStatus == PlanStatus.Running,
            cancellationToken);
        var runningPlanStage = runningPlanStages.FirstOrDefault();

        if (runningPlanStage is null)
        {
            return new AndonBoardDto
            {
                WorkStationId = workStation.Id,
                LineId = workStation.LineId,
                StageId = workStation.StageId,
                HasActivePlan = false,
            };
        }

        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(runningPlanStage.ProductionPlanId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {runningPlanStage.ProductionPlanId}.");

        var breakWindowEntities = await _unitOfWork.Repository<BreakWindow>().FindAsync(
            x => x.LineId == workStation.LineId, cancellationToken);
        var breakWindows = breakWindowEntities.Select(x => (x.StartTime, x.EndTime)).ToList();

        var scans = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.ProductionPlanId == productionPlan.Id && s.StageId == workStation.StageId, cancellationToken);

        // Scan.ScannedAtUtc lưu UTC -> quy đổi về giờ tường tại nhà máy (xem remarks AndonBoardCalculator) để
        // so sánh cùng hệ quy chiếu với ProductionPlan.StartTime/mốc giờ hiển thị.
        var okScannedAtLocal = scans
            .Where(s => s.Result == ScanResult.Ok)
            .Select(s => s.ScannedAtUtc.ToLocalTime())
            .OrderBy(t => t)
            .ToList();

        var ngCount = scans.Count(s => s.Result != ScanResult.Ok);
        var totalScanCount = scans.Count;

        var nowLocal = DateTime.Now;
        var standardQuantityPerHourExact = productionPlan.TaktTimeSeconds > 0
            ? SecondsPerHour / productionPlan.TaktTimeSeconds
            : 0m;

        var rows = new List<AndonBoardHourRowDto>();
        foreach (var mark in AndonBoardCalculator.BuildPastHourMarks(productionPlan.StartTime, nowLocal, MaxHourRows))
        {
            var planAtMark = AndonBoardCalculator.ComputePlanCumulative(standardQuantityPerHourExact, productionPlan.StartTime, mark, breakWindows);
            var actualAtMark = okScannedAtLocal.Count(t => t <= mark);
            rows.Add(BuildRow(mark, planAtMark, actualAtMark, isCurrent: false));
        }

        var planNow = AndonBoardCalculator.ComputePlanCumulative(standardQuantityPerHourExact, productionPlan.StartTime, nowLocal, breakWindows);
        var actualNow = okScannedAtLocal.Count;
        rows.Add(BuildRow(nowLocal, planNow, actualNow, isCurrent: true));

        return new AndonBoardDto
        {
            WorkStationId = workStation.Id,
            LineId = workStation.LineId,
            StageId = workStation.StageId,
            HasActivePlan = true,
            ProductionPlanId = productionPlan.Id,
            ActualCumulative = actualNow,
            PlanCumulative = planNow,
            Balance = actualNow - planNow,
            NgCount = ngCount,
            NgPercent = totalScanCount > 0 ? Math.Round(ngCount * 100m / totalScanCount, 1) : 0m,
            GeneratedAtLocal = nowLocal,
            Rows = rows,
        };
    }

    private static AndonBoardHourRowDto BuildRow(DateTime timeMark, int plan, int actual, bool isCurrent) => new()
    {
        TimeMarkLocal = timeMark,
        PlanCumulative = plan,
        ActualCumulative = actual,
        Balance = actual - plan,
        IsCurrent = isCurrent,
    };
}
