using System.Net;
using System.Net.Http;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.Reports;

/// <inheritdoc cref="ILotReportApiClient"/>
public class LotReportApiClient : ApiClientBase, ILotReportApiClient
{
    public LotReportApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<LotSummaryDto?> GetSummaryAsync(string lot, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/reports/lots/{Uri.EscapeDataString(lot)}");
        try
        {
            return await SendAsync<LotSummaryDto>(request, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // AC2 (US-21): "Không tìm thấy Lot" — Lot hoàn toàn mới, KHÔNG phải lỗi hệ thống.
            return null;
        }
    }
}
