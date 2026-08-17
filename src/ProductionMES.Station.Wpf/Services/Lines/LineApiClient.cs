using System.Net.Http;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.Lines;

/// <inheritdoc cref="ILineApiClient"/>
public class LineApiClient : ApiClientBase, ILineApiClient
{
    public LineApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<LineDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/lines");
        return await SendAsync<IReadOnlyList<LineDto>>(request, cancellationToken);
    }
}
