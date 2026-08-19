using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.ReworkUnlocks;

/// <summary>
/// <inheritdoc cref="IReworkUnlockApiClient"/>
/// 18/08/2026 (AC7): HttpClient của class này KHÔNG còn gắn <c>SupervisorAuthHandler</c> (xem đăng ký DI ở
/// <c>App.xaml.cs</c>) — nếu còn gắn, handler đó sẽ tự ghi đè header <c>Authorization</c> bằng token của
/// <c>ISupervisorSessionService</c> (phiên dùng chung) nếu phiên đó đang có hiệu lực cho chức năng Tổ trưởng
/// khác, làm sai lệch danh tính "Người sửa hàng". Token ephemeral truyền vào từng phương thức được gắn thủ công.
/// </summary>
public class ReworkUnlockApiClient : ApiClientBase, IReworkUnlockApiClient
{
    public ReworkUnlockApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<ReworkUnlockDto> UnlockAsync(string tagCode, int workStationId, string? note, string supervisorAccessToken, CancellationToken cancellationToken = default)
    {
        var request = new ReworkUnlockRequest { TagCode = tagCode, WorkStationId = workStationId, Note = note };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/scans/rework-unlock")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supervisorAccessToken);
        return await SendAsync<ReworkUnlockDto>(httpRequest, cancellationToken);
    }

    public async Task<ReworkLockStatusDto> GetLockStatusAsync(string tagCode, int workStationId, string supervisorAccessToken, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/scans/rework-unlock/status?workStationId={workStationId}&tagCode={Uri.EscapeDataString(tagCode)}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supervisorAccessToken);
        return await SendAsync<ReworkLockStatusDto>(httpRequest, cancellationToken);
    }
}
