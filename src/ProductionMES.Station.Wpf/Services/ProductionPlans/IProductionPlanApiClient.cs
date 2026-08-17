using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.ProductionPlans;

/// <summary>Gọi <c>api/v1/production-plans</c> (US-05) — yêu cầu đã đăng nhập Supervisor (ADR-005).</summary>
public interface IProductionPlanApiClient
{
    Task<IReadOnlyList<ProductionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductionPlanDto> CreateAsync(CreateProductionPlanRequest request, CancellationToken cancellationToken = default);

    Task<ProductionPlanDto> UpdateAsync(int id, UpdateProductionPlanRequest request, CancellationToken cancellationToken = default);
}
