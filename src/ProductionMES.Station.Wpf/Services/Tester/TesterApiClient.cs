using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.Tester;

/// <inheritdoc cref="ITesterApiClient"/>
public class TesterApiClient : ApiClientBase, ITesterApiClient
{
    private readonly StationOptions _options;

    public TesterApiClient(HttpClient httpClient, StationOptions options) : base(httpClient)
    {
        _options = options;
    }

    /// <summary>Cùng lý do <c>ScanApiClient</c>: 401 ở các endpoint StationApiKey luôn là lỗi cấu hình API Key của trạm.</summary>
    protected override string UnauthorizedMessage =>
        "Trạm chưa được cấp API Key hợp lệ (kiểm tra StationApiKeyValue trong appsettings.json của trạm) hoặc API Key đã bị thu hồi — không liên quan tới đăng nhập Tổ trưởng.";

    public async Task<TesterStationStateDto> GetStationStateAsync(CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, "api/v1/tester/station-state");
        return await SendAsync<TesterStationStateDto>(httpRequest, cancellationToken);
    }

    public async Task<TesterStationStateDto> SetEnabledAsync(bool enabled, string reason, string supervisorAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new SetTesterEnabledRequest { WorkStationId = _options.WorkStationId, Enabled = enabled, Reason = reason };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, "api/v1/tester/enabled")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supervisorAccessToken);

        try
        {
            return await SendAsync<TesterStationStateDto>(httpRequest, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiException(ex.StatusCode, "Phiên đăng nhập Tổ trưởng dùng để đổi chế độ máy thông điện đã hết hạn — vui lòng bấm lại công tắc để đăng nhập lại.");
        }
    }

    public async Task RecordEventAsync(RecordTesterEventRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/tester/events")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        await SendAsync(httpRequest, cancellationToken);
    }
}
