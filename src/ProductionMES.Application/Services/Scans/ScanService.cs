using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.Common;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Application.Services.ReworkUnlocks;
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
///
/// US-18 (<see cref="CreateNgAsync"/>): KHÁC <see cref="CreateAsync"/> — Scan NG là hành động XÁC NHẬN CHỦ ĐỘNG
/// của người vận hành (đã tự quyết định sản phẩm lỗi qua Chế độ Scan NG ở UI trạm), không phải kết quả suy luận
/// tự động của FR-08, nên KHÔNG chạy lại 2 bước kiểm tra chống trùng tem/công đoạn liền trước. Vẫn yêu cầu có 1
/// kế hoạch đang Running cho (Line, Công đoạn) của trạm (giống CreateAsync) để gắn đúng ProductionPlanId + snapshot
/// 6 field (US-10), phục vụ báo cáo PPM theo kế hoạch sau này (US-20). Cũng KHÔNG kiểm tra "đang khóa rework" (US-19)
/// — Tổ trưởng có thể chủ động xác nhận NG lần nữa cho 1 tem đang bị khóa/đã Ok, đây là đánh giá chất lượng chủ
/// động, độc lập với luồng tự động của <see cref="CreateAsync"/>.
///
/// US-19 (<see cref="ScanResult.WaitingReworkUnlock"/>): <see cref="CreateAsync"/> nay có thêm bước kiểm tra "tem
/// có đang bị khóa rework tại công đoạn này hay không" (<see cref="ReworkLockCalculator.IsLocked"/>) — chạy TRƯỚC
/// bước chống trùng tem (US-08 AC1), vì đây là mô tả trạng thái cụ thể hơn "trùng tem" khi cả 2 điều kiện cùng
/// đúng (tem có Ok VÀ đang khóa do 1 lần Ng sau đó — hiếm, nhưng ưu tiên thông báo rõ "đang chờ mở khóa rework").
/// Tái sử dụng CHUNG 1 truy vấn Scan theo (TagCode, StageId) cho cả 2 bước (khóa + trùng tem) để không tăng thêm
/// số lần gọi <c>IRepository&lt;Scan&gt;.FindAsync</c> so với trước khi có US-19.
/// </remarks>
public class ScanService : IScanService
{
    // US-10 AC2/AC3: chưa có FluentValidation validator riêng cho query GET (query string, không phải body) —
    // Service tự chỉnh Page/PageSize không hợp lệ về giá trị mặc định thay vì trả lỗi 400 (xem GetHistoryAsync).
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;

    /// <summary>US-18 AC4: giới hạn số lý do gợi ý trả về, tránh danh sách autocomplete quá dài khi công đoạn đã tích lũy nhiều lý do khác nhau theo thời gian.</summary>
    private const int MaxNgReasonSuggestions = 20;

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

        // US-08 AC1 + US-19 AC1 — Bước 1: tra cứu TOÀN BỘ lượt scan tại (TagCode, StageId) TOÀN HỆ THỐNG (không
        // phân biệt Line) 1 LẦN DUY NHẤT, dùng chung cho cả 2 kiểm tra bên dưới (khóa rework + trùng tem) — tránh
        // tăng thêm số lần gọi FindAsync so với trước khi có US-19.
        var scansAtCurrentStage = await scanRepository.FindAsync(
            s => s.TagCode == tagCode && s.StageId == workStation.StageId, cancellationToken);

        // US-19 AC1: tem đang bị khóa rework (lượt Ng gần nhất tại công đoạn này chưa được Tổ trưởng mở khóa) ->
        // từ chối ngay, không scan lại tự động được (FR-19). Kiểm tra TRƯỚC bước chống trùng tem — mô tả trạng
        // thái cụ thể hơn khi cả 2 điều kiện cùng đúng.
        var reworkUnlocksAtCurrentStage = await _unitOfWork.Repository<ReworkUnlock>().FindAsync(
            u => u.TagCode == tagCode && u.StageId == workStation.StageId, cancellationToken);

        if (ReworkLockCalculator.IsLocked(scansAtCurrentStage, reworkUnlocksAtCurrentStage))
        {
            var lockedScan = BuildScan(tagCode, workStation, activeProductionPlan, ScanResult.WaitingReworkUnlock,
                "Sản phẩm đang chờ mở khóa rework.", nowUtc);
            return await SaveAndReturnAsync(lockedScan, cancellationToken);
        }

        // US-08 AC1 — Bước 2: chống trùng tem theo (TagCode, StageId) TOÀN HỆ THỐNG, không phân biệt Line.
        var duplicateAtCurrentStage = scansAtCurrentStage.Where(s => s.Result == ScanResult.Ok).ToList();

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

    public async Task<ScanResultDto> CreateNgAsync(
        int workStationId, string tagCode, string rejectionReason, int confirmedByUserId, string confirmedByUserName, CancellationToken cancellationToken = default)
    {
        // FluentValidationActionFilter đã validate NotEmpty ở tầng request (CreateNgScanRequestValidator) — kiểm
        // tra lại ở đây phòng Service được gọi trực tiếp từ nơi khác không qua Controller (vd unit test/tái sử dụng).
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new BusinessRuleException("Phải nhập lý do lỗi trước khi ghi nhận Scan NG.");
        }

        // US-18 (thay đổi 18/08/2026): người xác nhận LUÔN phải có (Controller chỉ gọi tới đây sau khi xác thực
        // Bearer token Tổ trưởng thành công) — kiểm tra lại phòng Service được gọi trực tiếp không qua Controller.
        if (confirmedByUserId <= 0 || string.IsNullOrWhiteSpace(confirmedByUserName))
        {
            throw new BusinessRuleException("Thiếu thông tin người xác nhận Scan NG (yêu cầu đăng nhập Tổ trưởng hợp lệ).");
        }

        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");

        // Giống CreateAsync: cần đúng 1 kế hoạch đang Running cho (Line, Công đoạn) của trạm để gắn ProductionPlanId
        // + snapshot 6 field (US-10) — Scan NG vẫn thuộc về 1 kế hoạch cụ thể để phục vụ báo cáo PPM sau này (US-20).
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

        // AC3: KHÔNG chạy 2 bước kiểm tra (chống trùng tem/công đoạn liền trước) như CreateAsync — người vận
        // hành CHỦ ĐỘNG xác nhận đây là sản phẩm lỗi, không phải luồng tự động phát hiện của FR-08. Tem bị khóa ở
        // công đoạn kế tiếp tự động xảy ra vì công đoạn này không có bản ghi Ok (xem IScanService.CreateNgAsync).
        var ngScan = BuildScan(
            tagCode, workStation, activeProductionPlan, ScanResult.Ng, rejectionReason.Trim(), DateTime.UtcNow,
            confirmedByUserId, confirmedByUserName);
        return await SaveAndReturnAsync(ngScan, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetNgReasonSuggestionsAsync(int stageId, CancellationToken cancellationToken = default)
    {
        var ngScans = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.StageId == stageId && s.Result == ScanResult.Ng, cancellationToken);

        // AC4: không trùng lặp, sắp theo lần dùng gần nhất trước (lý do mới nhập gần đây lên trước).
        return ngScans
            .Where(s => !string.IsNullOrWhiteSpace(s.RejectionReason))
            .GroupBy(s => s.RejectionReason!)
            .Select(g => new { Reason = g.Key, LastUsedUtc = g.Max(s => s.ScannedAtUtc) })
            .OrderByDescending(x => x.LastUsedUtc)
            .Select(x => x.Reason)
            .Take(MaxNgReasonSuggestions)
            .ToList();
    }

    public async Task<PagedResult<ScanHistoryItemDto>> GetHistoryAsync(ScanHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : query.PageSize;

        // US-10 AC3: kết hợp AND toàn bộ filter tùy chọn trong 1 Expression duy nhất — EF Core dịch thẳng thành
        // WHERE khi chạy trên DB thật; unit test mock IRepository<Scan>.FindAsync bằng cách compile + lọc trên
        // list in-memory (đúng cách ScanServiceTests hiện có đang mô phỏng repository thật).
        var matched = await _unitOfWork.Repository<Scan>().FindAsync(
            s =>
                (query.TagCode == null || s.TagCode == query.TagCode) &&
                (query.WorkStationId == null || s.WorkStationId == query.WorkStationId) &&
                (query.LineId == null || s.LineId == query.LineId) &&
                (query.FromUtc == null || s.ScannedAtUtc >= query.FromUtc) &&
                (query.ToUtc == null || s.ScannedAtUtc <= query.ToUtc),
            cancellationToken);

        // AC2: "sắp xếp theo thời gian" -> tăng dần (thứ tự xảy ra trước-sau của các lượt scan cùng 1 tem).
        var ordered = matched.OrderBy(s => s.ScannedAtUtc).ThenBy(s => s.Id).ToList();

        var pageItems = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToHistoryItemDto)
            .ToList();

        return new PagedResult<ScanHistoryItemDto>
        {
            Items = pageItems,
            TotalCount = ordered.Count,
            Page = page,
            PageSize = pageSize,
        };
    }

    private static ScanHistoryItemDto ToHistoryItemDto(Scan scan) => new()
    {
        Id = scan.Id,
        TagCode = scan.TagCode,
        StageId = scan.StageId,
        LineId = scan.LineId,
        WorkStationId = scan.WorkStationId,
        ProductionPlanId = scan.ProductionPlanId,
        Customer = scan.Customer,
        Model = scan.Model,
        Lot = scan.Lot,
        Revision = scan.Revision,
        PlannedQuantity = scan.PlannedQuantity,
        TaktTimeSeconds = scan.TaktTimeSeconds,
        ScannedAtUtc = scan.ScannedAtUtc,
        Result = scan.Result,
        RejectionReason = scan.RejectionReason,
        ConfirmedByUserId = scan.ConfirmedByUserId,
        ConfirmedByUserName = scan.ConfirmedByUserName,
    };

    private static Scan BuildScan(
        string tagCode, WorkStation workStation, ProductionPlan productionPlan, ScanResult result, string? rejectionReason, DateTime scannedAtUtc,
        int? confirmedByUserId = null, string? confirmedByUserName = null)
        => new()
        {
            TagCode = tagCode,
            StageId = workStation.StageId,
            LineId = workStation.LineId,
            WorkStationId = workStation.Id,
            ProductionPlanId = productionPlan.Id,
            // US-10: snapshot bất biến tại thời điểm scan (xem remarks tại entity Scan) — không tra cứu động qua ProductionPlanId.
            Customer = productionPlan.Customer,
            Model = productionPlan.Model,
            Lot = productionPlan.Lot,
            Revision = productionPlan.Revision,
            PlannedQuantity = productionPlan.PlannedQuantity,
            TaktTimeSeconds = productionPlan.TaktTimeSeconds,
            ScannedAtUtc = scannedAtUtc,
            Result = result,
            RejectionReason = rejectionReason,
            // US-18 (thay đổi 18/08/2026): chỉ CreateNgAsync truyền 2 tham số này — mọi nhánh khác (CreateAsync) giữ null (xem remarks tại entity Scan).
            ConfirmedByUserId = confirmedByUserId,
            ConfirmedByUserName = confirmedByUserName,
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
        Customer = scan.Customer,
        Model = scan.Model,
        Lot = scan.Lot,
        Revision = scan.Revision,
        PlannedQuantity = scan.PlannedQuantity,
        TaktTimeSeconds = scan.TaktTimeSeconds,
        ScannedAtUtc = scan.ScannedAtUtc,
        Result = scan.Result,
        RejectionReason = scan.RejectionReason,
        ConfirmedByUserId = scan.ConfirmedByUserId,
        ConfirmedByUserName = scan.ConfirmedByUserName,
    };
}
