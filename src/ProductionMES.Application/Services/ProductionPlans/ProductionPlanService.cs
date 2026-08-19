using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Application.Services.Lots;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.ProductionPlans;

/// <summary>
/// Implementation IProductionPlanService (US-05/FR-05, cập nhật 13/08/2026; AC7-AC9 US-05, viết lại hoàn toàn
/// 19/08/2026 theo US-21a — "Tổng số lượng Lot" NHẬP TAY qua <see cref="ILotService"/>, KHÔNG phải SUM).
/// US-06/FR-06: StandardQuantityPerHour tính lúc map Entity → DTO (ToDtoAsync), không lưu cột riêng trong DB.
/// Vòng đời trạng thái (Draft/Running/Paused/Completed/Cancelled) KHÔNG xử lý ở service này — xem
/// <see cref="ProductionPlanStages.ProductionPlanStageService"/> (US-05a, đúng entity đang giữ trạng thái đó).
/// </summary>
public class ProductionPlanService : IProductionPlanService
{
    private const decimal SecondsPerHour = 3600m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILotService _lotService;

    public ProductionPlanService(IUnitOfWork unitOfWork, ILotService lotService)
    {
        _unitOfWork = unitOfWork;
        _lotService = lotService;
    }

    public async Task<ProductionPlanDto> CreateAsync(CreateProductionPlanRequest request, string? updatedByUserName, CancellationToken cancellationToken = default)
    {
        // AC1: Line phải tồn tại và đang hoạt động mới được tạo kế hoạch
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(request.LineId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy Line với Id = {request.LineId}.");

        if (!line.IsActive)
        {
            throw new BusinessRuleException($"Line \"{line.Name}\" đang ngưng hoạt động, không thể tạo kế hoạch sản xuất mới.");
        }

        // US-05 AC7 (=US-21a AC1): bắt buộc "Tổng số lượng Lot" khi Lot hoàn toàn mới — kiểm tra TRƯỚC khi tạo
        // kế hoạch (chính kế hoạch đang tạo chưa persist nên chưa tính vào "đã có ProductionPlan").
        await EnsureLotTotalQuantityAsync(request.Lot, request.LotTotalQuantity, request.Confirm, updatedByUserName, cancellationToken);

        var productionPlan = new ProductionPlan
        {
            LineId = request.LineId,
            Customer = request.Customer,
            Model = request.Model,
            Lot = request.Lot,
            Revision = request.Revision,
            PlannedQuantity = request.PlannedQuantity,
            TaktTimeSeconds = request.TaktTimeSeconds,
            StartTime = request.StartTime,
            OperatorNames = request.OperatorNames,
        };

        var repository = _unitOfWork.Repository<ProductionPlan>();
        await repository.AddAsync(productionPlan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(productionPlan, cancellationToken);
    }

    public async Task<ProductionPlanDto> UpdateAsync(int id, UpdateProductionPlanRequest request, string? updatedByUserName, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<ProductionPlan>();
        var productionPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {id}.");

        // AC5: sửa Số lượng kế hoạch/Takt time trong khi kế hoạch đã có công đoạn Running/Paused là tình huống
        // "chạy dở" — cần Tổ trưởng xác nhận rõ ràng trước khi lưu, tránh sửa nhầm làm sai lệch cách tính "còn
        // lại" (US-05a AC4) hoặc chỉ số +/- đang hiển thị tại trạm.
        var quantityOrTaktTimeChanged =
            request.PlannedQuantity != productionPlan.PlannedQuantity ||
            request.TaktTimeSeconds != productionPlan.TaktTimeSeconds;

        if (quantityOrTaktTimeChanged && !request.Confirm)
        {
            var runningOrPausedStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
                x => x.ProductionPlanId == id && (x.PlanStatus == PlanStatus.Running || x.PlanStatus == PlanStatus.Paused),
                cancellationToken);

            if (runningOrPausedStages.Count > 0)
            {
                throw new BusinessRuleException(
                    "Kế hoạch này đang chạy dở (có công đoạn Running/Paused) — sửa Số lượng kế hoạch/Takt time có " +
                    "thể làm sai lệch cách tính 'còn lại' hoặc chỉ số +/- đang hiển thị tại trạm. Xác nhận thay đổi " +
                    "bằng cách gửi lại yêu cầu với Confirm = true.");
            }
        }

        // AC6 (US-05): khóa TUYỆT ĐỐI sửa Customer/Model/Lot/Revision nếu kế hoạch đã có ít nhất 1 bản ghi Scan
        // — khác hẳn cơ chế Confirm=true ở trên (Quantity/TaktTime), rule này KHÔNG có đường Confirm để bỏ qua,
        // không lọc theo PlanStatus/Result, vì lịch sử scan cũ đã gắn snapshot (US-10) với đúng giá trị hiện tại,
        // sửa sẽ làm traceability sai lệch giữa "kế hoạch hiện tại" và "kế hoạch tại thời điểm đã scan".
        var identityFieldsChanged =
            request.Customer != productionPlan.Customer ||
            request.Model != productionPlan.Model ||
            request.Lot != productionPlan.Lot ||
            request.Revision != productionPlan.Revision;

        if (identityFieldsChanged)
        {
            var existingScans = await _unitOfWork.Repository<Scan>().FindAsync(
                s => s.ProductionPlanId == id, cancellationToken);

            if (existingScans.Count > 0)
            {
                throw new BusinessRuleException(
                    "Kế hoạch này đã có lịch sử scan gắn với Khách hàng/Model/Lot/Revision hiện tại — không thể " +
                    "sửa các thông tin này để tránh sai lệch dữ liệu lịch sử. Nếu nhập sai, hãy tạo kế hoạch mới.");
            }
        }

        // US-05 AC7 (=US-21a AC1): bắt buộc "Tổng số lượng Lot" khi Lot (sau khi sửa) hoàn toàn mới — dùng CHUNG
        // cơ chế với CreateAsync, tính theo request.Lot (có thể đã đổi so với Lot cũ nếu chưa có scan — AC6 ở trên).
        await EnsureLotTotalQuantityAsync(request.Lot, request.LotTotalQuantity, request.Confirm, updatedByUserName, cancellationToken);

        // AC4: kế hoạch chưa từng Running ở công đoạn nào (hoặc không đổi Số lượng/Takt time) -> cập nhật tự do.
        productionPlan.Customer = request.Customer;
        productionPlan.Model = request.Model;
        productionPlan.Lot = request.Lot;
        productionPlan.Revision = request.Revision;
        productionPlan.PlannedQuantity = request.PlannedQuantity;
        productionPlan.TaktTimeSeconds = request.TaktTimeSeconds;
        productionPlan.StartTime = request.StartTime;
        productionPlan.OperatorNames = request.OperatorNames;

        repository.Update(productionPlan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(productionPlan, cancellationToken);
    }

    public async Task<ProductionPlanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(id, cancellationToken);
        return productionPlan is null ? null : await ToDtoAsync(productionPlan, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var productionPlans = await _unitOfWork.Repository<ProductionPlan>().GetAllAsync(cancellationToken);
        if (productionPlans.Count == 0)
        {
            return Array.Empty<ProductionPlanDto>();
        }

        // AC9/US-21a: tra "Tổng số lượng Lot" theo tập hợp Code, tránh N+1 khi liệt kê nhiều kế hoạch cùng lúc.
        var codes = productionPlans.Select(p => p.Lot).Distinct().ToList();
        var lotByCode = await _lotService.GetByCodesAsync(codes, cancellationToken);
        return productionPlans.Select(p => ToDto(p, lotByCode.TryGetValue(p.Lot, out var lotDto) ? lotDto.TotalQuantity : null)).ToList();
    }

    /// <summary>
    /// US-05 AC7 (=US-21a AC1): xác định "Lot hoàn toàn mới" (chưa từng có <see cref="ProductionPlan"/> nào khác
    /// dùng Code này) TRƯỚC khi validate bắt buộc — nếu mới mà <paramref name="lotTotalQuantity"/> = null, từ
    /// chối. Nếu có giá trị (Lot mới hoặc đã tồn tại), upsert qua <see cref="ILotService"/> (AC8: soft-confirm nếu
    /// giảm dưới thực tế, dùng chung <paramref name="confirm"/>).
    /// </summary>
    private async Task EnsureLotTotalQuantityAsync(
        string lot, int? lotTotalQuantity, bool confirm, string? updatedByUserName, CancellationToken cancellationToken)
    {
        var isNewLot = !await _lotService.HasAnyProductionPlanAsync(lot, cancellationToken);
        if (isNewLot && lotTotalQuantity is null)
        {
            throw new BusinessRuleException(
                $"Lot \"{lot}\" hoàn toàn mới (chưa từng có kế hoạch sản xuất nào trước đó) — bắt buộc nhập " +
                "\"Tổng số lượng Lot\" trước khi lưu kế hoạch.");
        }

        if (lotTotalQuantity.HasValue)
        {
            await _lotService.UpsertTotalQuantityAsync(lot, lotTotalQuantity.Value, confirm, updatedByUserName, cancellationToken);
        }
    }

    /// <summary>US-06/FR-06/AC-04: Sản lượng chuẩn/giờ = 3600 / Takt time (vd takt = 30s -> 120 sản phẩm/giờ). AC7/AC9: nạp kèm "Tổng số lượng Lot" hiện có.</summary>
    private async Task<ProductionPlanDto> ToDtoAsync(ProductionPlan productionPlan, CancellationToken cancellationToken)
    {
        var lotDto = await _lotService.GetByCodeAsync(productionPlan.Lot, cancellationToken);
        return ToDto(productionPlan, lotDto?.TotalQuantity);
    }

    private static ProductionPlanDto ToDto(ProductionPlan productionPlan, int? lotTotalQuantity) => new()
    {
        Id = productionPlan.Id,
        LineId = productionPlan.LineId,
        Customer = productionPlan.Customer,
        Model = productionPlan.Model,
        Lot = productionPlan.Lot,
        Revision = productionPlan.Revision,
        PlannedQuantity = productionPlan.PlannedQuantity,
        TaktTimeSeconds = productionPlan.TaktTimeSeconds,
        StartTime = productionPlan.StartTime,
        OperatorNames = productionPlan.OperatorNames,
        StandardQuantityPerHour = productionPlan.TaktTimeSeconds > 0 ? Math.Round(SecondsPerHour / productionPlan.TaktTimeSeconds, 2) : 0,
        LotTotalQuantity = lotTotalQuantity,
    };
}
