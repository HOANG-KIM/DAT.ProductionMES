using System.Net.Http;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.WorkStations;

/// <inheritdoc cref="IWorkStationApiClient"/>
public class WorkStationApiClient : ApiClientBase, IWorkStationApiClient
{
    public WorkStationApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<WorkStationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/work-stations");
        return await SendAsync<IReadOnlyList<WorkStationDto>>(request, cancellationToken);
    }
}
