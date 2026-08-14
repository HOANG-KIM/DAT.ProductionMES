using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Scans;

/// <summary>
/// Implementation IScanService (US-07/US-08, FR-07/FR-08; cập nhật 14/08/2026 theo US-05a/FR-05a).
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
/// US-05a: "kế hoạch active của trạm" nay được xác định là cặp (Line, Công đoạn) của trạm đang có 1
/// <see cref="ProductionPlanStage"/> ở trạng thái <see cref="PlanStatus.Running"/> — KHÔNG còn dựa vào
/// <c>ProductionPlan.IsActive</c> (đã bỏ, xem remarks tại entity ProductionPlan). Vì ProductionPlanStage đại
/// diện đúng cặp (Kế hoạch, Công đoạn), việc tìm được 1 bản ghi Running cũng đồng thời xác nhận công đoạn của
/// trạm ĐÃ được cấu hình trong kế hoạch đó — không cần kiểm tra "chưa cấu hình" như 1 bước tách biệt nữa.
///
/// "Không có kế hoạch nào đang Running cho (Line, Công đoạn) của trạm" là lỗi cấu hình/vận hành (không phải 1
/// trong 3 giá trị <see cref="ScanResult"/> đã chốt cho FR-08) — xử lý bằng <see cref="BusinessRuleException"/>
/// (HTTP 409), KHÔNG lưu bản ghi Scan cho trường hợp này (khác với DuplicateTag/PreviousStageNotPassed — 2 kết
/// quả nghiệp vụ hợp lệ theo FR-08, luôn được lưu theo FR-10).
///
/// US-05a AC5: sau khi lưu 1 lượt scan OK, tự động chuyển <see cref="ProductionPlanStage.PlanStatus"/> của đúng
/// cặp (Kế hoạch, Công đoạn) đó sang <see cref="PlanStatus.Completed"/> ngay khi số lượt scan OK (tính động,
/// gồm cả lượt vừa lưu) đạt đủ <c>ProductionPlan.PlannedQuantity</c>.
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

        // US-05a/mục 6 quy tắc 12: tại 1 thời điểm, 1 cặp (Line, Công đoạn) chỉ có tối đa 1 kế hoạch Running.
        var productionPlanStageRepository = _unitOfWork.Repository<ProductionPlanStage>();
        var runningPlanStages = await productionPlanStageRepository.FindAsync(
            x => x.LineId == workStation.LineId && x.StageId == workStation.StageId && x.PlanStatus == PlanStatus.Running,
            cancellationToken);
        var runningPlanStage = runningPlanStages.FirstOrDefault();

        if (runningPlanStage is null)
        {
            throw new BusinessRuleException(
                $"(Line Id = {workStation.LineId}, Công đoạn Id = {workStation.StageId}) hiện không có kế hoạch sản xuất " +
                "nào đang Running — không thể ghi nhận lượt scan.");
        }

        var activeProductionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(runningPlanStage.ProductionPlanId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {runningPlanStage.ProductionPlanId}.");

        // Suy ra trình tự + PreviousStageId của đúng công đoạn trạm này đang thực hiện, trong kế hoạch đang Running.
        var planStages = await _productionPlanStageService.GetByProductionPlanAsync(activeProductionPlan.Id, cancellationToken);
        var currentPlanStage = planStages.FirstOrDefault(x => x.StageId == workStation.StageId);

        if (currentPlanStage is null)
        {
            // Phòng vệ: runningPlanStage vừa tìm thấy ở trên đã xác nhận công đoạn này được cấu hình trong kế
            // hoạch, nên nhánh này chỉ xảy ra khi dữ liệu bất nhất (vd race condition xóa cấu hình đồng thời).
            throw new BusinessRuleException(
                $"Công đoạn của trạm Id = {workStationId} chưa được cấu hình trong kế hoạch sản xuất Id = {activeProductionPlan.Id}.");
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

        // US-05a AC5: tự động Completed khi số lượt scan OK (tính động, gồm cả lượt vừa lưu) đạt đủ số lượng kế hoạch.
        var runCount = (await scanRepository.FindAsync(
            s => s.ProductionPlanId == activeProductionPlan.Id && s.StageId == workStation.StageId && s.Result == ScanResult.Ok,
            cancellationToken)).Count;

        if (runCount >= activeProductionPlan.PlannedQuantity)
        {
            runningPlanStage.PlanStatus = PlanStatus.Completed;
            productionPlanStageRepository.Update(runningPlanStage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

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
