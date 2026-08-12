using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.ProductionPlans;

/// <summary>
/// Implementation IProductionPlanService (US-05/FR-05).
/// AC2 (US-05): 1 Line chỉ có 1 kế hoạch active tại 1 thời điểm — validate ở <see cref="ActivateAsync"/>
/// trước khi cho kích hoạt, không phải ở <see cref="CreateAsync"/> (vì tạo mới luôn ở trạng thái chưa active).
/// US-06/FR-06: StandardQuantityPerHour tính lúc map Entity → DTO (ToDto), không lưu cột riêng trong DB.
/// </summary>
public class ProductionPlanService : IProductionPlanService
{
    private const decimal SecondsPerHour = 3600m;

    private readonly IUnitOfWork _unitOfWork;

    public ProductionPlanService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductionPlanDto> CreateAsync(CreateProductionPlanRequest request, CancellationToken cancellationToken = default)
    {
        // AC1: Line phải tồn tại và đang hoạt động mới được tạo kế hoạch
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(request.LineId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy Line với Id = {request.LineId}.");

        if (!line.IsActive)
        {
            throw new BusinessRuleException($"Line \"{line.Name}\" đang ngưng hoạt động, không thể tạo kế hoạch sản xuất mới.");
        }

        var productionPlan = new ProductionPlan
        {
            LineId = request.LineId,
            ProductCode = request.ProductCode,
            ProductName = request.ProductName,
            PlannedQuantity = request.PlannedQuantity,
            TaktTimeSeconds = request.TaktTimeSeconds,
            Shift = request.Shift,
            EffectiveDate = request.EffectiveDate,
            IsActive = false, // AC1: tạo mới chưa active, kích hoạt là thao tác riêng
        };

        var repository = _unitOfWork.Repository<ProductionPlan>();
        await repository.AddAsync(productionPlan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(productionPlan);
    }

    public async Task<ProductionPlanDto> UpdateAsync(int id, UpdateProductionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<ProductionPlan>();
        var productionPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {id}.");

        // AC3: cập nhật thông tin, không đụng tới LineId hay trạng thái active
        productionPlan.ProductCode = request.ProductCode;
        productionPlan.ProductName = request.ProductName;
        productionPlan.PlannedQuantity = request.PlannedQuantity;
        productionPlan.TaktTimeSeconds = request.TaktTimeSeconds;
        productionPlan.Shift = request.Shift;
        productionPlan.EffectiveDate = request.EffectiveDate;

        repository.Update(productionPlan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(productionPlan);
    }

    public async Task<ProductionPlanDto> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<ProductionPlan>();
        var productionPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {id}.");

        if (productionPlan.IsActive)
        {
            return ToDto(productionPlan);
        }

        // AC2: 1 Line chỉ có 1 kế hoạch active tại 1 thời điểm
        var productionPlansOnSameLine = await repository.FindAsync(
            k => k.LineId == productionPlan.LineId && k.IsActive && k.Id != productionPlan.Id,
            cancellationToken);

        if (productionPlansOnSameLine.Count > 0)
        {
            throw new BusinessRuleException(
                $"Line này đang có 1 kế hoạch khác (Id = {productionPlansOnSameLine[0].Id}) ở trạng thái active. " +
                "Cần kết thúc/chuyển trạng thái kế hoạch cũ trước khi kích hoạt kế hoạch mới.");
        }

        productionPlan.IsActive = true;

        repository.Update(productionPlan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(productionPlan);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<ProductionPlan>();
        var productionPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {id}.");

        productionPlan.IsActive = false;

        repository.Update(productionPlan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductionPlanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(id, cancellationToken);
        return productionPlan is null ? null : ToDto(productionPlan);
    }

    public async Task<IReadOnlyList<ProductionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var productionPlans = await _unitOfWork.Repository<ProductionPlan>().GetAllAsync(cancellationToken);
        return productionPlans.Select(ToDto).ToList();
    }

    /// <summary>US-06/FR-06/AC-04: Sản lượng chuẩn/giờ = 3600 / Takt time (vd takt = 30s -> 120 sản phẩm/giờ).</summary>
    private static ProductionPlanDto ToDto(ProductionPlan productionPlan) => new()
    {
        Id = productionPlan.Id,
        LineId = productionPlan.LineId,
        ProductCode = productionPlan.ProductCode,
        ProductName = productionPlan.ProductName,
        PlannedQuantity = productionPlan.PlannedQuantity,
        TaktTimeSeconds = productionPlan.TaktTimeSeconds,
        Shift = productionPlan.Shift,
        EffectiveDate = productionPlan.EffectiveDate,
        IsActive = productionPlan.IsActive,
        StandardQuantityPerHour = productionPlan.TaktTimeSeconds > 0 ? Math.Round(SecondsPerHour / productionPlan.TaktTimeSeconds, 2) : 0,
    };
}
