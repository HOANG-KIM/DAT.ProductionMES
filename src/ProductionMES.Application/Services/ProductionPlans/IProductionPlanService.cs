using ProductionMES.Application.DTOs.ProductionPlans;

namespace ProductionMES.Application.Services.ProductionPlans;

/// <summary>Service quản lý kế hoạch sản xuất (US-05/FR-05, kèm US-06/FR-06 tính sản lượng chuẩn/giờ).</summary>
public interface IProductionPlanService
{
    /// <summary>Tạo mới 1 kế hoạch, luôn ở trạng thái chưa active (AC1).</summary>
    Task<ProductionPlanDto> CreateAsync(CreateProductionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật thông tin 1 kế hoạch đã tồn tại (AC3).</summary>
    Task<ProductionPlanDto> UpdateAsync(int id, UpdateProductionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kích hoạt 1 kế hoạch. Từ chối nếu Line tương ứng đã có 1 kế hoạch khác đang active (AC2) — Tổ trưởng
    /// cần kết thúc/chuyển trạng thái kế hoạch cũ (<see cref="DeactivateAsync"/>) trước khi kích hoạt kế hoạch mới.
    /// </summary>
    Task<ProductionPlanDto> ActivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Kết thúc (ngưng active) 1 kế hoạch đang active.</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductionPlanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
