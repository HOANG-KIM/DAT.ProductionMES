using ProductionMES.Application.DTOs.ProductionPlans;

namespace ProductionMES.Application.Services.ProductionPlans;

/// <summary>
/// Service quản lý thông tin kế hoạch sản xuất (US-05/FR-05, kèm US-06/FR-06 tính sản lượng chuẩn/giờ).
/// KHÔNG có action kích hoạt/kết thúc ở đây — vòng đời trạng thái (Draft/Running/Paused/Completed/Cancelled)
/// gắn theo cặp (Kế hoạch, Công đoạn), xem <see cref="ProductionPlanStages.IProductionPlanStageService.ApplyAsync"/>/
/// <c>PauseAsync</c>/<c>CloseAsync</c> (US-05a).
/// </summary>
public interface IProductionPlanService
{
    /// <summary>Tạo mới 1 kế hoạch (AC1) — tự nhiên ở trạng thái "Draft" vì chưa có công đoạn nào được áp dụng.</summary>
    Task<ProductionPlanDto> CreateAsync(CreateProductionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin 1 kế hoạch đã tồn tại (AC4/AC5). Nếu kế hoạch đã có công đoạn Running/Paused và
    /// request đổi Số lượng kế hoạch/Takt time mà <see cref="UpdateProductionPlanRequest.Confirm"/> = false,
    /// từ chối kèm cảnh báo (AC5) — gọi lại với <c>Confirm = true</c> để xác nhận.
    /// </summary>
    Task<ProductionPlanDto> UpdateAsync(int id, UpdateProductionPlanRequest request, CancellationToken cancellationToken = default);

    Task<ProductionPlanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
