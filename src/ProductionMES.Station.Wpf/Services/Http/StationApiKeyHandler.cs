using System.Net.Http;
using ProductionMES.Station.Wpf.Configuration;

namespace ProductionMES.Station.Wpf.Services.Http;

/// <summary>
/// Tự gắn header <c>X-Station-Api-Key</c> cho luồng scan thường tại trạm (ADR-005, scheme <c>StationApiKey</c>)
/// — khác hoàn toàn <see cref="SupervisorAuthHandler"/> (Bearer, luồng Tổ trưởng nâng quyền). Đọc giá trị thô từ
/// <see cref="StationOptions.StationApiKeyValue"/> (cấu hình cục bộ tại trạm) tại thời điểm gửi, không cache lại.
/// </summary>
public class StationApiKeyHandler : DelegatingHandler
{
    private readonly StationOptions _options;

    public StationApiKeyHandler(StationOptions options)
    {
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_options.StationApiKeyValue is { Length: > 0 } apiKey)
        {
            request.Headers.Remove(StationApiKeyHeaderName);
            request.Headers.Add(StationApiKeyHeaderName, apiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Mirror <c>StationApiKeyDefaults.HeaderName</c> phía backend (ADR-005) — Station.Wpf không reference
    /// project backend nào, nên chép lại đúng tên header tại đây.
    /// </summary>
    private const string StationApiKeyHeaderName = "X-Station-Api-Key";
}
