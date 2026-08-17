using System.Net.Http;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.ProductionPlans;

/// <inheritdoc cref="IProductionPlanApiClient"/>
public class ProductionPlanApiClient : ApiClientBase, IProductionPlanApiClient
{
    public ProductionPlanApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<ProductionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/production-plans");
        return await SendAsync<IReadOnlyList<ProductionPlanDto>>(request, cancellationToken);
    }

    public async Task<ProductionPlanDto> CreateAsync(CreateProductionPlanRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/production-plans")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        return await SendAsync<ProductionPlanDto>(httpRequest, cancellationToken);
    }

    public async Task<ProductionPlanDto> UpdateAsync(int id, UpdateProductionPlanRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"api/v1/production-plans/{id}")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        return await SendAsync<ProductionPlanDto>(httpRequest, cancellationToken);
    }
}
