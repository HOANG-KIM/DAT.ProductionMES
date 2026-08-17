using System.Net.Http;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.ProductionPlanStages;

/// <inheritdoc cref="IProductionPlanStageApiClient"/>
public class ProductionPlanStageApiClient : ApiClientBase, IProductionPlanStageApiClient
{
    public ProductionPlanStageApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<ProductionPlanStageSelectionDto>> GetByLineAndStageAsync(
        int lineId, int stageId, bool includeClosed = false, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/production-plan-stages?lineId={lineId}&stageId={stageId}&includeClosed={includeClosed}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<IReadOnlyList<ProductionPlanStageSelectionDto>>(request, cancellationToken);
    }

    public async Task<ProductionPlanStageDto> ApplyAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/production-plans/{productionPlanId}/stages/{stageId}/apply");
        return await SendAsync<ProductionPlanStageDto>(request, cancellationToken);
    }

    public async Task<ProductionPlanStageDto> PauseAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/production-plans/{productionPlanId}/stages/{stageId}/pause");
        return await SendAsync<ProductionPlanStageDto>(request, cancellationToken);
    }

    public async Task<ProductionPlanStageDto> CloseAsync(int productionPlanId, int stageId, bool confirm, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/production-plans/{productionPlanId}/stages/{stageId}/close")
        {
            Content = JsonContent.Create(new CloseProductionPlanStageRequest { Confirm = confirm }, options: JsonDefaults.Options),
        };
        return await SendAsync<ProductionPlanStageDto>(request, cancellationToken);
    }
}
