using System.Net.Http;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.Scans;

/// <inheritdoc cref="IScanApiClient"/>
public class ScanApiClient : ApiClientBase, IScanApiClient
{
    public ScanApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <summary>
    /// 401 ở endpoint này (scheme "StationApiKey", ADR-005) không liên quan gì tới đăng nhập Tổ trưởng — nguyên
    /// nhân thực tế luôn là thiếu/sai <c>StationApiKeyValue</c> (cấu hình cục bộ tại trạm, xem
    /// <c>StationApiKeyHandler</c>) hoặc API Key đã bị thu hồi/không thuộc đúng <c>WorkStationId</c> phía server.
    /// </summary>
    protected override string UnauthorizedMessage =>
        "Trạm chưa được cấp API Key hợp lệ (kiểm tra StationApiKeyValue trong appsettings.json của trạm) hoặc API Key đã bị thu hồi — không liên quan tới đăng nhập Tổ trưởng.";

    public async Task<ScanResultDto> CreateAsync(string tagCode, int workStationId, CancellationToken cancellationToken = default)
    {
        var request = new CreateScanRequest { TagCode = tagCode, WorkStationId = workStationId };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/scans")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        return await SendAsync<ScanResultDto>(httpRequest, cancellationToken);
    }
}
