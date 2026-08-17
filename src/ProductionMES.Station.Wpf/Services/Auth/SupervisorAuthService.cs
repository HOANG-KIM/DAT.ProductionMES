using System.Net.Http;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.Auth;

/// <inheritdoc cref="ISupervisorAuthService"/>
public class SupervisorAuthService : ApiClientBase, ISupervisorAuthService
{
    private readonly ISupervisorSessionService _sessionService;

    public SupervisorAuthService(HttpClient httpClient, ISupervisorSessionService sessionService)
        : base(httpClient)
    {
        _sessionService = sessionService;
    }

    public async Task LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/station-login")
        {
            Content = JsonContent.Create(new LoginRequest { Username = username, Password = password }, options: JsonDefaults.Options),
        };

        var result = await SendAsync<StationLoginResponse>(request, cancellationToken);

        _sessionService.SetSession(
            result.AccessToken, result.AccessTokenExpiresAtUtc,
            result.RefreshToken, result.RefreshTokenExpiresAtUtc,
            result.Username, result.FullName);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = _sessionService.RefreshToken;
        _sessionService.Clear();

        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/station-logout")
            {
                Content = JsonContent.Create(new { refreshToken }, options: JsonDefaults.Options),
            };
            await SendAsync(request, cancellationToken);
        }
        catch (ApiException)
        {
            // Đăng xuất phải luôn thành công từ góc nhìn UI (session cục bộ đã xoá) — lỗi mạng/server ở bước
            // thu hồi token phía server không nên chặn Tổ trưởng thoát khỏi màn hình cấu hình.
        }
    }
}
