using ProductionMES.Application.DTOs.ProductionPlanStages;

namespace ProductionMES.Application.Services.ProductionPlanStages;

/// <summary>Service cấu hình công đoạn áp dụng cho từng kế hoạch sản xuất, kèm trình tự (US-03/FR-03).</summary>
public interface IProductionPlanStageService
{
    /// <summary>Thêm 1 công đoạn từ danh mục master vào kế hoạch (AC1).</summary>
    Task<ProductionPlanStageDto> AddAsync(int productionPlanId, AddStageToProductionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gỡ 1 công đoạn khỏi kế hoạch, tự động điều chỉnh lại trình tự các công đoạn còn lại (AC2).</summary>
    Task RemoveAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sắp xếp lại toàn bộ trình tự công đoạn của kế hoạch (AC3). Từ chối nếu trùng số thứ tự (AC4) hoặc
    /// cấu hình dẫn tới vòng lặp (AC5).
    /// </summary>
    Task<IReadOnlyList<ProductionPlanStageDto>> ReorderAsync(int productionPlanId, ReorderProductionPlanStageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách công đoạn (kèm trình tự) đã cấu hình cho 1 kế hoạch, sắp theo SequenceNumber.</summary>
    Task<IReadOnlyList<ProductionPlanStageDto>> GetByProductionPlanAsync(int productionPlanId, CancellationToken cancellationToken = default);
}
