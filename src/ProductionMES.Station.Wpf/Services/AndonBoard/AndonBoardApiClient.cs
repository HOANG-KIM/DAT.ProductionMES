using System.Net.Http;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.AndonBoard;

/// <inheritdoc cref="IAndonBoardApiClient"/>
public class AndonBoardApiClient : ApiClientBase, IAndonBoardApiClient
{
    public AndonBoardApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    /// <summary>401 ở endpoint này (scheme "StationApiKey") không liên quan đăng nhập Tổ trưởng — cùng lý do đã ghi ở <c>ScanApiClient</c>.</summary>
    protected override string UnauthorizedMessage =>
        "Trạm chưa được cấp API Key hợp lệ (kiểm tra StationApiKeyValue trong appsettings.json của trạm) hoặc API Key đã bị thu hồi — không liên quan tới đăng nhập Tổ trưởng.";

    public async Task<AndonBoardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/andon-board");
        return await SendAsync<AndonBoardDto>(request, cancellationToken);
    }
}
