using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Scans;

/// <summary>
/// Implementation IScanService (US-07/US-08, FR-07/FR-08).
/// </summary>
/// <remarks>
/// Tái sử dụng <see cref="IProductionPlanStageService.GetByProductionPlanAsync"/> để suy ra "công đoạn liền
/// trước" (<c>ProductionPlanStageDto.PreviousStageId</c> đã được <c>ProductionPlanStageService</c> tính sẵn từ
/// <c>SequenceNumber - 1</c> trong cùng kế hoạch — xem remarks tại entity <see cref="ProductionPlanStage"/>),
/// thay vì viết lại thuật toán suy luận trình tự ở đây.
///
/// Cả 2 bước kiểm tra của FR-08 (chống trùng tem, đã qua công đoạn liền trước) đều tra cứu bảng Scan theo
/// <c>StageId</c> (công đoạn master) trên TOÀN HỆ THỐNG — không lọc theo Line/kế hoạch — đúng quy tắc đã chốt
/// (CLAUDE.md, mục 6 SRS).
///
/// "Không có kế hoạch sản xuất đang active" hoặc "công đoạn của trạm chưa được cấu hình trong kế hoạch active"
/// là lỗi cấu hình/vận hành (không phải 1 trong 3 giá trị <see cref="ScanResult"/> đã chốt cho FR-08) — xử lý
/// bằng <see cref="BusinessRuleException"/> (HTTP 409), KHÔNG lưu bản ghi Scan cho 2 trường hợp này (khác với
/// DuplicateTag/PreviousStageNotPassed — 2 kết quả nghiệp vụ hợp lệ theo FR-08, luôn được lưu theo FR-10).
/// </remarks>
public class ScanService : IScanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductionPlanStageService _productionPlanStageService;
    private readonly IScanNotifier _scanNotifier;

    public ScanService(
        IUnitOfWork unitOfWork,
        IProductionPlanStageService productionPlanStageService,
        IScanNotifier scanNotifier)
    {
        _unitOfWork = unitOfWork;
        _productionPlanStageService = productionPlanStageService;
        _scanNotifier = scanNotifier;
    }

    public async Task<ScanResultDto> CreateAsync(int workStationId, string tagCode, CancellationToken cancellationToken = default)
    {
        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");

        // FR-05: 1 Line tại 1 thời điểm chỉ có tối đa 1 kế hoạch active.
        var productionPlanRepository = _unitOfWork.Repository<ProductionPlan>();
        var activeProductionPlans = await productionPlanRepository.FindAsync(
            p => p.LineId == workStation.LineId && p.IsActive, cancellationToken);
        var activeProductionPlan = activeProductionPlans.FirstOrDefault();

        if (activeProductionPlan is null)
        {
            throw new BusinessRuleException(
                $"Line Id = {workStation.LineId} hiện không có kế hoạch sản xuất đang active — không thể ghi nhận lượt scan.");
        }

        // Suy ra trình tự + PreviousStageId của đúng công đoạn trạm này đang thực hiện, trong kế hoạch active.
        var planStages = await _productionPlanStageService.GetByProductionPlanAsync(activeProductionPlan.Id, cancellationToken);
        var currentPlanStage = planStages.FirstOrDefault(x => x.StageId == workStation.StageId);

        if (currentPlanStage is null)
        {
            throw new BusinessRuleException(
                $"Công đoạn của trạm Id = {workStationId} chưa được cấu hình trong kế hoạch sản xuất đang active (Id = {activeProductionPlan.Id}).");
        }

        var scanRepository = _unitOfWork.Repository<Scan>();
        var nowUtc = DateTime.UtcNow;

        // US-08 AC1 — Bước 1: chống trùng tem theo (TagCode, StageId) TOÀN HỆ THỐNG, không phân biệt Line.
        var duplicateAtCurrentStage = await scanRepository.FindAsync(
            s => s.TagCode == tagCode && s.StageId == workStation.StageId && s.Result == ScanResult.Ok,
            cancellationToken);

        if (duplicateAtCurrentStage.Count > 0)
        {
            var rejectedScan = BuildScan(tagCode, workStation, activeProductionPlan, ScanResult.DuplicateTag,
                "Trùng tem tại công đoạn này.", nowUtc);
            return await SaveAndReturnAsync(rejectedScan, cancellationToken);
        }

        // US-08 AC3 — Bước 2: đã qua công đoạn liền trước hay chưa, cũng tra cứu TOÀN HỆ THỐNG.
        // currentPlanStage.PreviousStageId = null khi đây là công đoạn đầu tiên (SequenceNumber = 1) -> bỏ qua bước này.
        if (currentPlanStage.PreviousStageId is not null)
        {
            var previousStageId = currentPlanStage.PreviousStageId.Value;
            var passedPreviousStage = await scanRepository.FindAsync(
                s => s.TagCode == tagCode && s.StageId == previousStageId && s.Result == ScanResult.Ok,
                cancellationToken);

            if (passedPreviousStage.Count == 0)
            {
                var previousStage = await _unitOfWork.Repository<Stage>().GetByIdAsync(previousStageId, cancellationToken);
                var previousStageName = previousStage?.Name ?? $"#{previousStageId}";

                var rejectedScan = BuildScan(tagCode, workStation, activeProductionPlan, ScanResult.PreviousStageNotPassed,
                    $"Chưa qua công đoạn: {previousStageName}", nowUtc);
                return await SaveAndReturnAsync(rejectedScan, cancellationToken);
            }
        }

        // US-08 AC4: qua đủ 2 bước kiểm tra -> ghi nhận OK.
        var okScan = BuildScan(tagCode, workStation, activeProductionPlan, ScanResult.Ok, rejectionReason: null, nowUtc);
        var result = await SaveAndReturnAsync(okScan, cancellationToken);

        // US-07 AC2/AC3: bắn sự kiện real-time cho trạm CHỈ khi lượt scan OK — hạ tầng để Station.Wpf tương lai
        // subscribe (UI thật hiển thị tại trạm chưa triển khai ở đợt US-07/US-08 này, xem báo cáo bàn giao).
        await _scanNotifier.NotifyScanRecordedAsync(workStationId, result, cancellationToken);

        return result;
    }

    private static Scan BuildScan(
        string tagCode, WorkStation workStation, ProductionPlan productionPlan, ScanResult result, string? rejectionReason, DateTime scannedAtUtc)
        => new()
        {
            TagCode = tagCode,
            StageId = workStation.StageId,
            LineId = workStation.LineId,
            WorkStationId = workStation.Id,
            ProductionPlanId = productionPlan.Id,
            ScannedAtUtc = scannedAtUtc,
            Result = result,
            RejectionReason = rejectionReason,
        };

    private async Task<ScanResultDto> SaveAndReturnAsync(Scan scan, CancellationToken cancellationToken)
    {
        // FR-10: mọi lượt scan (kể cả bị từ chối) đều được lưu lại — không có khái niệm "return lỗi mà không lưu DB".
        await _unitOfWork.Repository<Scan>().AddAsync(scan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(scan);
    }

    private static ScanResultDto ToDto(Scan scan) => new()
    {
        TagCode = scan.TagCode,
        StageId = scan.StageId,
        LineId = scan.LineId,
        WorkStationId = scan.WorkStationId,
        ProductionPlanId = scan.ProductionPlanId,
        ScannedAtUtc = scan.ScannedAtUtc,
        Result = scan.Result,
        RejectionReason = scan.RejectionReason,
    };
}
