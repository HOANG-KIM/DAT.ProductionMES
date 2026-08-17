using System.Net.Http;
using System.Net.Http.Headers;
using ProductionMES.Station.Wpf.Services.Auth;

namespace ProductionMES.Station.Wpf.Services.Http;

/// <summary>
/// Tự gắn header <c>Authorization: Bearer {accessToken}</c> cho mọi request qua HttpClient "SupervisorClient"
/// (ADR-005) — đọc token từ <see cref="ISupervisorSessionService"/> tại thời điểm gửi, không cache lại ở đây.
/// </summary>
public class SupervisorAuthHandler : DelegatingHandler
{
    private readonly ISupervisorSessionService _sessionService;

    public SupervisorAuthHandler(ISupervisorSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_sessionService.AccessToken is { Length: > 0 } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
