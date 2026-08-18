using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

    /// <summary>
    /// US-18 (thay đổi 18/08/2026): <paramref name="supervisorAccessToken"/> gắn tường minh vào header
    /// <c>Authorization: Bearer</c> của đúng request này — KHÔNG dùng <c>SupervisorAuthHandler</c>/
    /// <c>ISupervisorSessionService</c> (HttpClient của <see cref="ScanApiClient"/> vẫn gắn
    /// <c>StationApiKeyHandler</c> cho <see cref="CreateAsync"/>/<see cref="GetNgReasonSuggestionsAsync"/>, header
    /// <c>X-Station-Api-Key</c> đó bị endpoint Bearer-only này bỏ qua, vô hại) — token dùng riêng cho 1 lượt Scan
    /// NG, không lưu ở session dùng chung (xem <c>AndonBoardViewModel</c>).
    /// </summary>
    public async Task<ScanResultDto> CreateNgAsync(string tagCode, int workStationId, string rejectionReason, string supervisorAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new CreateNgScanRequest { TagCode = tagCode, WorkStationId = workStationId, RejectionReason = rejectionReason };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/scans/ng")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supervisorAccessToken);

        try
        {
            return await SendAsync<ScanResultDto>(httpRequest, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // US-18: 401 ở đây KHÁC UnauthorizedMessage mặc định của class này (StationApiKey, xem override bên
            // dưới) — nguyên nhân thật là token Tổ trưởng dùng riêng cho lượt Scan NG này đã hết hạn/không hợp lệ
            // (hiếm, vì đăng nhập vừa xong ở AC2a) — thông báo rõ để người vận hành biết cần bấm lại nút "NG".
            throw new ApiException(ex.StatusCode, "Phiên đăng nhập Tổ trưởng dùng để xác nhận NG đã hết hạn — vui lòng bấm lại nút NG để đăng nhập lại.");
        }
    }

    public async Task<IReadOnlyList<string>> GetNgReasonSuggestionsAsync(int stageId, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"api/v1/scans/ng-reasons?stageId={stageId}");
        return await SendAsync<IReadOnlyList<string>>(httpRequest, cancellationToken);
    }
}
