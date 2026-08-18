using System.Net.Http;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.ReworkUnlocks;

/// <inheritdoc cref="IReworkUnlockApiClient"/>
public class ReworkUnlockApiClient : ApiClientBase, IReworkUnlockApiClient
{
    public ReworkUnlockApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<ReworkUnlockDto> UnlockAsync(string tagCode, int workStationId, string? note, CancellationToken cancellationToken = default)
    {
        var request = new ReworkUnlockRequest { TagCode = tagCode, WorkStationId = workStationId, Note = note };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/scans/rework-unlock")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        return await SendAsync<ReworkUnlockDto>(httpRequest, cancellationToken);
    }

    public async Task<ReworkLockStatusDto> GetLockStatusAsync(string tagCode, int workStationId, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/scans/rework-unlock/status?workStationId={workStationId}&tagCode={Uri.EscapeDataString(tagCode)}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<ReworkLockStatusDto>(httpRequest, cancellationToken);
    }
}
