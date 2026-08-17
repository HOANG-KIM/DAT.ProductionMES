using System.Net.Http;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.Stages;

/// <inheritdoc cref="IStageApiClient"/>
public class StageApiClient : ApiClientBase, IStageApiClient
{
    public StageApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<StageDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/stages");
        return await SendAsync<IReadOnlyList<StageDto>>(request, cancellationToken);
    }
}
