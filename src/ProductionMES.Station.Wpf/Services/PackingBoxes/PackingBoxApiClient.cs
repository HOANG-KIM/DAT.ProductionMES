using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.PackingBoxes;

/// <inheritdoc cref="IPackingBoxApiClient"/>
public class PackingBoxApiClient : ApiClientBase, IPackingBoxApiClient
{
    private const string BasePath = "api/v1/packing-boxes";

    public PackingBoxApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <summary>Cùng lý do <c>ScanApiClient</c>: 401 ở scheme StationApiKey không liên quan đăng nhập Tổ trưởng.</summary>
    protected override string UnauthorizedMessage =>
        "Trạm chưa được cấp API Key hợp lệ (kiểm tra StationApiKeyValue trong appsettings.json của trạm) hoặc API Key đã bị thu hồi — không liên quan tới đăng nhập Tổ trưởng.";

    public async Task<PackingBoxStateDto> GetStateAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BasePath}/state");
        return await SendAsync<PackingBoxStateDto>(request, cancellationToken);
    }

    public async Task<PackingBoxDto> SetStartingBoxNoAsync(int startingBoxNo, CancellationToken cancellationToken = default)
    {
        var body = new SetStartingBoxNoRequest { StartingBoxNo = startingBoxNo };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/starting-box-no")
        {
            Content = JsonContent.Create(body, options: JsonDefaults.Options),
        };
        return await SendAsync<PackingBoxDto>(request, cancellationToken);
    }

    /// <summary>
    /// AC7: KHÁC <see cref="SetStartingBoxNoAsync"/> — endpoint <c>POST api/v1/packing-boxes/box-no</c> yêu cầu
    /// Bearer Tổ trưởng (cùng lý do <c>ScanApiClient.CreateNgAsync</c>) thay vì StationApiKey — token gắn tường
    /// minh vào ĐÚNG request này, không dùng <c>SupervisorAuthHandler</c>.
    /// </summary>
    public async Task<PackingBoxDto> UpdateCurrentBoxNoAsync(int workStationId, int newBoxNo, string supervisorAccessToken, CancellationToken cancellationToken = default)
    {
        var body = new UpdateCurrentBoxNoRequest { WorkStationId = workStationId, NewBoxNo = newBoxNo };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/box-no")
        {
            Content = JsonContent.Create(body, options: JsonDefaults.Options),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supervisorAccessToken);

        try
        {
            return await SendAsync<PackingBoxDto>(request, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiException(ex.StatusCode, "Phiên đăng nhập Tổ trưởng dùng để sửa số thùng đã hết hạn — vui lòng thử lại.");
        }
    }

    public async Task<PackingDuplicateConfirmationDto> ConfirmDuplicateAsync(
        int workStationId, string tagCode, string? note, string supervisorAccessToken, CancellationToken cancellationToken = default)
    {
        var body = new ConfirmPackingDuplicateRequest { WorkStationId = workStationId, TagCode = tagCode, Note = note };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/duplicate-confirmations")
        {
            Content = JsonContent.Create(body, options: JsonDefaults.Options),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supervisorAccessToken);

        try
        {
            return await SendAsync<PackingDuplicateConfirmationDto>(request, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiException(ex.StatusCode, "Phiên đăng nhập Tổ trưởng dùng để xác nhận tem trùng đã hết hạn — vui lòng thử lại.");
        }
    }

    public async Task DownloadLabelAsync(int boxId, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync($"{BasePath}/{boxId}/label", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ToApiExceptionAsync(response, cancellationToken);
        }

        await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await httpStream.CopyToAsync(fileStream, cancellationToken);
    }
}
