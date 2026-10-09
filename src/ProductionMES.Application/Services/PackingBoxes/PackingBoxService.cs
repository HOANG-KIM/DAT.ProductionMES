using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Storage;
using ProductionMES.Application.DTOs.PackingBoxes;
using ProductionMES.Application.Services.PackingModelConfigs;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.PackingBoxes;

/// <summary>Implementation <see cref="IPackingBoxService"/> (US-25/FR-25).</summary>
public class PackingBoxService : IPackingBoxService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPackingModelConfigService _packingModelConfigService;
    private readonly IPackingTemplateStorage _templateStorage;
    private readonly IPackingLabelGenerator _labelGenerator;

    public PackingBoxService(
        IUnitOfWork unitOfWork,
        IPackingModelConfigService packingModelConfigService,
        IPackingTemplateStorage templateStorage,
        IPackingLabelGenerator labelGenerator)
    {
        _unitOfWork = unitOfWork;
        _packingModelConfigService = packingModelConfigService;
        _templateStorage = templateStorage;
        _labelGenerator = labelGenerator;
    }

    public async Task<PackingBoxStateDto> GetStateAsync(int workStationId, CancellationToken cancellationToken = default)
    {
        var workStation = await GetWorkStationAsync(workStationId, cancellationToken);
        var productionPlan = await FindRunningProductionPlanAsync(workStation, cancellationToken);

        var boxes = await _unitOfWork.Repository<PackingBox>().FindAsync(
            b => b.ProductionPlanId == productionPlan.Id && b.StageId == workStation.StageId, cancellationToken);

        var currentBox = boxes
            .Where(b => b.Status == PackingBoxStatus.InProgress)
            .OrderByDescending(b => b.BoxNo)
            .FirstOrDefault();

        var lastCompletedBox = boxes
            .Where(b => b.Status == PackingBoxStatus.Completed)
            .OrderByDescending(b => b.CompletedAtUtc)
            .ThenByDescending(b => b.Id)
            .FirstOrDefault();

        var modelLotBoxes = await GetBoxesInSameModelLotAsync(productionPlan, cancellationToken);

        return new PackingBoxStateDto
        {
            RequiresStartingBoxNo = boxes.Count == 0,
            CurrentBox = currentBox is null ? null : ToDto(currentBox),
            LastCompletedBox = lastCompletedBox is null ? null : ToDto(lastCompletedBox),
            Model = productionPlan.Model,
            Lot = productionPlan.Lot,
            SuggestedNextBoxNo = SuggestNextBoxNo(modelLotBoxes, excludedBox: currentBox),
        };
    }

    public async Task<PackingBoxDto> SetStartingBoxNoAsync(int workStationId, int startingBoxNo, CancellationToken cancellationToken = default)
    {
        if (startingBoxNo <= 0)
        {
            throw new BusinessRuleException("Số thùng bắt đầu phải lớn hơn 0.");
        }

        var workStation = await GetWorkStationAsync(workStationId, cancellationToken);
        var productionPlan = await FindRunningProductionPlanAsync(workStation, cancellationToken);

        var existingBoxes = await _unitOfWork.Repository<PackingBox>().FindAsync(
            b => b.ProductionPlanId == productionPlan.Id && b.StageId == workStation.StageId, cancellationToken);

        if (existingBoxes.Count > 0)
        {
            // AC5: chỉ bắt buộc nhập 1 LẦN DUY NHẤT cho lần đầu đóng thùng của kế hoạch — các thùng kế tiếp tự
            // động (AC4/AC6), sửa lại số thùng hiện tại dùng AC7 (UpdateCurrentBoxNoAsync), không phải endpoint này.
            throw new BusinessRuleException(
                "Kế hoạch này đã có thùng được mở tại công đoạn Đóng thùng — không thể nhập lại số thùng bắt đầu (dùng chức năng Sửa số thùng nếu cần điều chỉnh).");
        }

        var config = await GetPackingModelConfigOrThrowAsync(productionPlan.Model, cancellationToken);

        var modelLotBoxes = await GetBoxesInSameModelLotAsync(productionPlan, cancellationToken);
        EnsureBoxNoNotUsed(startingBoxNo, modelLotBoxes, excludedBox: null, productionPlan);

        var box = new PackingBox
        {
            ProductionPlanId = productionPlan.Id,
            LineId = workStation.LineId,
            StageId = workStation.StageId,
            WorkStationId = workStation.Id,
            BoxNo = startingBoxNo,
            Status = PackingBoxStatus.InProgress,
            TargetQuantity = config.PackingQuantity,
            ScannedQuantity = 0,
            PackingModelConfigId = config.Id,
            ModelSnapshot = config.Model,
            PartNameSnapshot = config.PartName,
            ManufacturerSnapshot = config.Manufacturer,
            GrossWeightSnapshot = config.GrossWeight,
            OpenedAtUtc = DateTime.Now,
        };

        var repository = _unitOfWork.Repository<PackingBox>();
        await repository.AddAsync(box, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(box);
    }

    public async Task<PackingBoxDto> UpdateCurrentBoxNoAsync(
        int workStationId, int newBoxNo, int updatedByUserId, string updatedByUserName, CancellationToken cancellationToken = default)
    {
        if (newBoxNo <= 0)
        {
            throw new BusinessRuleException("Số thùng phải lớn hơn 0.");
        }

        // Phòng vệ Service bị gọi trực tiếp không qua Controller (cùng idiom ScanService.CreateNgAsync, US-18).
        if (updatedByUserId <= 0 || string.IsNullOrWhiteSpace(updatedByUserName))
        {
            throw new BusinessRuleException("Thiếu thông tin người thực hiện sửa số thùng (yêu cầu đăng nhập Tổ trưởng hợp lệ).");
        }

        var workStation = await GetWorkStationAsync(workStationId, cancellationToken);
        var productionPlan = await FindRunningProductionPlanAsync(workStation, cancellationToken);

        var currentBox = (await _unitOfWork.Repository<PackingBox>().FindAsync(
                b => b.ProductionPlanId == productionPlan.Id && b.StageId == workStation.StageId && b.Status == PackingBoxStatus.InProgress,
                cancellationToken))
            .OrderByDescending(b => b.BoxNo)
            .FirstOrDefault();

        if (currentBox is null)
        {
            throw new BusinessRuleException(
                "Hiện chưa có thùng nào đang mở tại công đoạn Đóng thùng để sửa số thùng — vui lòng nhập số thùng bắt đầu trước.");
        }

        var modelLotBoxes = await GetBoxesInSameModelLotAsync(productionPlan, cancellationToken);
        EnsureBoxNoNotUsed(newBoxNo, modelLotBoxes, excludedBox: currentBox, productionPlan);

        // AC7: chỉ đổi nhãn BoxNo — KHÔNG đổi ScannedQuantity/TargetQuantity đang có của thùng.
        currentBox.BoxNo = newBoxNo;

        var repository = _unitOfWork.Repository<PackingBox>();
        repository.Update(currentBox);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(currentBox);
    }

    // US-27 (25/08/2026): ConfirmDuplicateAsync (US-25 AC8) đã bị XÓA — xem ghi chú tại IPackingBoxService.

    public async Task<(byte[] Content, string FileName)> GenerateLabelAsync(int boxId, CancellationToken cancellationToken = default)
    {
        var box = await _unitOfWork.Repository<PackingBox>().GetByIdAsync(boxId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy thùng với Id = {boxId}.");

        // AC13 "thiếu template": Model của thùng này chưa từng có ai tải lên mẫu tem — đây LÀ lỗi của chính lệnh
        // gọi in (không phải lỗi vật lý máy in), phải chặn/báo lỗi rõ ràng cho Station.Wpf.
        await using var templateStream = await _templateStorage.OpenReadAsync(box.PackingModelConfigId, cancellationToken)
            ?? throw new BusinessRuleException(
                $"Model \"{box.ModelSnapshot}\" chưa có mẫu tem (template) — vui lòng cấu hình mẫu tem trước khi in tem thùng.");

        using var templateBuffer = new MemoryStream();
        await templateStream.CopyToAsync(templateBuffer, cancellationToken);

        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(box.WorkStationId, cancellationToken);
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(box.LineId, cancellationToken);
        // US-28: Khách hàng/Lot/Nhân viên đọc từ kế hoạch lúc tạo tem (không snapshot trên PackingBox) — Customer/Lot
        // đã bị khoá khi kế hoạch có scan (FR-05), riêng OperatorNames sửa được nên "In lại" thùng cũ sau khi Tổ
        // trưởng đổi tên nhân viên sẽ in tên MỚI.
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(box.ProductionPlanId, cancellationToken);

        var data = new PackingLabelData
        {
            Model = box.ModelSnapshot,
            PartName = box.PartNameSnapshot,
            Manufacturer = box.ManufacturerSnapshot,
            PackingQuantity = box.TargetQuantity,
            GrossWeight = box.GrossWeightSnapshot,
            BoxNo = box.BoxNo,
            PackedAtLocal = box.CompletedAtUtc ?? DateTime.Now,
            LineName = line?.Name ?? $"#{box.LineId}",
            WorkStationName = workStation?.Name ?? $"#{box.WorkStationId}",
            Customer = productionPlan?.Customer ?? string.Empty,
            Lot = productionPlan?.Lot ?? string.Empty,
            OperatorNames = productionPlan?.OperatorNames ?? string.Empty,
        };

        var content = _labelGenerator.Generate(templateBuffer.ToArray(), data);
        var fileName = $"tem-thung-{SanitizeFileName(box.ModelSnapshot)}-box{box.BoxNo}.xlsx";
        return (content, fileName);
    }

    public async Task<PackingBox> EnsureReadyForScanAsync(WorkStation workStation, ProductionPlan productionPlan, CancellationToken cancellationToken = default)
    {
        // AC11: chặn TRƯỚC khi chạy 2 bước kiểm tra FR-08 — KHÔNG lưu bản ghi Scan nào cho lỗi cấu hình này (cùng
        // nguyên tắc "không có kế hoạch Running" hiện có ở ScanService).
        await GetPackingModelConfigOrThrowAsync(productionPlan.Model, cancellationToken);

        var currentBox = (await _unitOfWork.Repository<PackingBox>().FindAsync(
                b => b.ProductionPlanId == productionPlan.Id && b.StageId == workStation.StageId && b.Status == PackingBoxStatus.InProgress,
                cancellationToken))
            .OrderByDescending(b => b.BoxNo)
            .FirstOrDefault();

        // AC5: chưa từng nhập số thùng bắt đầu cho kế hoạch này -> chặn quét, KHÔNG lưu bản ghi Scan.
        return currentBox ?? throw new BusinessRuleException(
            "Chưa nhập số thùng bắt đầu cho kế hoạch này tại công đoạn Đóng thùng — vui lòng nhập số thùng bắt đầu trước khi quét tem.");
    }

    public async Task<PackingScanOutcome> RegisterOkScanAsync(PackingBox currentBox, CancellationToken cancellationToken = default)
    {
        currentBox.ScannedQuantity += 1;
        var repository = _unitOfWork.Repository<PackingBox>();

        // AC2: chưa đủ số lượng -> chỉ tăng đếm, thùng vẫn InProgress.
        if (currentBox.ScannedQuantity < currentBox.TargetQuantity)
        {
            repository.Update(currentBox);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PackingScanOutcome
            {
                BoxId = currentBox.Id,
                BoxNo = currentBox.BoxNo,
                ScannedQuantity = currentBox.ScannedQuantity,
                TargetQuantity = currentBox.TargetQuantity,
                BoxCompleted = false,
            };
        }

        // AC4: vừa đủ số lượng -> hoàn tất thùng này VÀ tự động mở thùng kế tiếp ngay (sẵn sàng nhận tem mới).
        currentBox.Status = PackingBoxStatus.Completed;
        currentBox.CompletedAtUtc = DateTime.Now;
        repository.Update(currentBox);

        // AC12: thùng MỚI snapshot Quy cách đóng gói HIỆN TẠI (có thể đã bị Admin sửa từ lúc mở thùng trước) —
        // đọc lại config theo đúng Model, KHÔNG tái sử dụng snapshot cũ của currentBox.
        var freshConfig = await _packingModelConfigService.GetByModelAsync(currentBox.ModelSnapshot, cancellationToken);

        // Số thùng kế tiếp = số nhỏ nhất > thùng vừa xong chưa có thùng nào khác cùng (Model, Lot) dùng — tránh trùng
        // khi dãy số có thùng đóng trước đó ở kế hoạch khác (vd đã có 1,2,4 rồi bắt đầu lại từ 3 -> kế tiếp là 5).
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(currentBox.ProductionPlanId, cancellationToken);
        var modelLotBoxes = productionPlan is null
            ? (IReadOnlyList<PackingBox>)Array.Empty<PackingBox>()
            : await GetBoxesInSameModelLotAsync(productionPlan, cancellationToken);
        var usedBoxNos = modelLotBoxes.Select(b => b.BoxNo).ToHashSet();
        var nextBoxNo = currentBox.BoxNo + 1;
        while (usedBoxNos.Contains(nextBoxNo))
        {
            nextBoxNo++;
        }

        var nextBox = new PackingBox
        {
            ProductionPlanId = currentBox.ProductionPlanId,
            LineId = currentBox.LineId,
            StageId = currentBox.StageId,
            WorkStationId = currentBox.WorkStationId,
            BoxNo = nextBoxNo,
            Status = PackingBoxStatus.InProgress,
            ScannedQuantity = 0,
            // Phòng vệ: config bị xoá giữa chừng (chưa có AC nào cho phép xoá hiện tại) -> giữ nguyên snapshot cũ
            // thay vì crash, để Operator vẫn tiếp tục đóng thùng kế tiếp được (AC13 tinh thần "không chặn").
            TargetQuantity = freshConfig?.PackingQuantity ?? currentBox.TargetQuantity,
            PackingModelConfigId = freshConfig?.Id ?? currentBox.PackingModelConfigId,
            ModelSnapshot = freshConfig?.Model ?? currentBox.ModelSnapshot,
            PartNameSnapshot = freshConfig?.PartName ?? currentBox.PartNameSnapshot,
            ManufacturerSnapshot = freshConfig?.Manufacturer ?? currentBox.ManufacturerSnapshot,
            GrossWeightSnapshot = freshConfig?.GrossWeight ?? currentBox.GrossWeightSnapshot,
            OpenedAtUtc = DateTime.Now,
        };

        await repository.AddAsync(nextBox, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PackingScanOutcome
        {
            BoxId = nextBox.Id,
            BoxNo = nextBox.BoxNo,
            ScannedQuantity = nextBox.ScannedQuantity,
            TargetQuantity = nextBox.TargetQuantity,
            BoxCompleted = true,
            CompletedBoxId = currentBox.Id,
        };
    }

    private async Task<WorkStation> GetWorkStationAsync(int workStationId, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");

    /// <summary>Cùng logic "1 (Line, Công đoạn) chỉ có tối đa 1 kế hoạch Running" đang dùng ở <c>ScanService.CreateAsync</c> (US-05a quy tắc 12).</summary>
    private async Task<ProductionPlan> FindRunningProductionPlanAsync(WorkStation workStation, CancellationToken cancellationToken)
    {
        var runningPlanStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            x => x.LineId == workStation.LineId && x.StageId == workStation.StageId && x.PlanStatus == PlanStatus.Running,
            cancellationToken);
        var runningPlanStage = runningPlanStages.FirstOrDefault();

        if (runningPlanStage is null)
        {
            throw new BusinessRuleException(
                $"(Line Id = {workStation.LineId}, Công đoạn Id = {workStation.StageId}) hiện không có kế hoạch sản xuất " +
                "nào đang Running — không thể thao tác đóng thùng.");
        }

        return await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(runningPlanStage.ProductionPlanId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {runningPlanStage.ProductionPlanId}.");
    }

    /// <summary>
    /// Toàn bộ thùng (mọi kế hoạch, mọi trạng thái) cùng (Model, Lot) với <paramref name="productionPlan"/> — dãy số
    /// thùng tính theo (Model, Lot), KHÔNG theo từng kế hoạch: 1 Lot có thể chạy qua nhiều kế hoạch (đổi Lot rồi mở
    /// lại), số thùng phải nối tiếp và không trùng. So khớp Model/Lot không phân biệt hoa/thường, bỏ khoảng trắng 2 đầu.
    /// </summary>
    private async Task<IReadOnlyList<PackingBox>> GetBoxesInSameModelLotAsync(ProductionPlan productionPlan, CancellationToken cancellationToken)
    {
        var modelKey = productionPlan.Model.Trim().ToUpper();
        var lotKey = productionPlan.Lot.Trim().ToUpper();

        var plans = await _unitOfWork.Repository<ProductionPlan>().FindAsync(
            p => p.Model.Trim().ToUpper() == modelKey && p.Lot.Trim().ToUpper() == lotKey, cancellationToken);
        var planIds = plans.Select(p => p.Id).Append(productionPlan.Id).Distinct().ToList();

        return await _unitOfWork.Repository<PackingBox>().FindAsync(b => planIds.Contains(b.ProductionPlanId), cancellationToken);
    }

    /// <summary>Số lớn nhất đã dùng + 1 (bỏ qua <paramref name="excludedBox"/> — thùng đang được sửa số), chưa có thùng nào thì 1.</summary>
    private static int SuggestNextBoxNo(IEnumerable<PackingBox> modelLotBoxes, PackingBox? excludedBox) =>
        modelLotBoxes.Where(b => excludedBox is null || b.Id != excludedBox.Id)
            .Select(b => b.BoxNo)
            .DefaultIfEmpty(0)
            .Max() + 1;

    /// <summary>Chặn dùng lại số thùng đã in (hoặc đang đóng ở kế hoạch khác) của cùng (Model, Lot).</summary>
    private static void EnsureBoxNoNotUsed(int boxNo, IReadOnlyList<PackingBox> modelLotBoxes, PackingBox? excludedBox, ProductionPlan productionPlan)
    {
        var conflict = modelLotBoxes.FirstOrDefault(b => b.BoxNo == boxNo && (excludedBox is null || b.Id != excludedBox.Id));
        if (conflict is null)
        {
            return;
        }

        var reason = conflict.Status == PackingBoxStatus.Completed ? "đã được in" : "đang được đóng ở kế hoạch khác";
        throw new BusinessRuleException(
            $"Thùng số {boxNo} của Model \"{productionPlan.Model}\" / Lot \"{productionPlan.Lot}\" {reason} — không thể dùng lại. " +
            $"Số thùng kế tiếp gợi ý: {SuggestNextBoxNo(modelLotBoxes, excludedBox)}.");
    }

    /// <summary>AC11: chặn nếu Model của kế hoạch chưa được cấu hình Quy cách đóng gói (US-24).</summary>
    private async Task<DTOs.PackingModelConfigs.PackingModelConfigDto> GetPackingModelConfigOrThrowAsync(string model, CancellationToken cancellationToken)
    {
        var config = await _packingModelConfigService.GetByModelAsync(model, cancellationToken);
        return config ?? throw new BusinessRuleException(
            $"Model \"{model}\" chưa được cấu hình Quy cách đóng gói — vui lòng cấu hình trước khi đóng thùng (US-24).");
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static PackingBoxDto ToDto(PackingBox box) => new()
    {
        Id = box.Id,
        ProductionPlanId = box.ProductionPlanId,
        StageId = box.StageId,
        WorkStationId = box.WorkStationId,
        BoxNo = box.BoxNo,
        Status = box.Status,
        TargetQuantity = box.TargetQuantity,
        ScannedQuantity = box.ScannedQuantity,
        Model = box.ModelSnapshot,
        PartName = box.PartNameSnapshot,
        Manufacturer = box.ManufacturerSnapshot,
        GrossWeight = box.GrossWeightSnapshot,
        OpenedAtUtc = box.OpenedAtUtc,
        CompletedAtUtc = box.CompletedAtUtc,
    };
}
