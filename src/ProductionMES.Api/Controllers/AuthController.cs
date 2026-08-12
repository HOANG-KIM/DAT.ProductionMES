using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Application.DTOs.Auth;
using ProductionMES.Application.Services.Auth;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Đăng nhập/đăng xuất, phát hành + thu hồi access/refresh token qua cookie HttpOnly (US-22, ADR-003).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private const string AccessTokenCookieName = "access_token";
    private const string RefreshTokenCookieName = "refresh_token";
    private const string RefreshTokenCookiePath = "/api/v1/auth";

    private readonly IAuthService _authService;
    private readonly IAntiforgery _antiforgery;

    public AuthController(IAuthService authService, IAntiforgery antiforgery)
    {
        _authService = authService;
        _antiforgery = antiforgery;
    }

    /// <summary>Đăng nhập bằng tên đăng nhập/mật khẩu, set cookie access_token + refresh_token nếu hợp lệ.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var tokens = await _authService.LoginAsync(request, cancellationToken);
        if (tokens is null)
        {
            return Unauthorized();
        }

        SetAuthCookies(tokens);

        return Ok(ToLoginResponse(tokens));
    }

    /// <summary>
    /// Đổi cặp access/refresh token mới dựa trên cookie refresh_token hiện có (rotation). Refresh_token không
    /// hợp lệ/đã thu hồi/hết hạn → xóa cookie, trả 401 — client phải đăng nhập lại.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshTokenRaw) || string.IsNullOrEmpty(refreshTokenRaw))
        {
            return Unauthorized();
        }

        var tokens = await _authService.RefreshAsync(refreshTokenRaw, cancellationToken);
        if (tokens is null)
        {
            DeleteAuthCookies();
            return Unauthorized();
        }

        SetAuthCookies(tokens);

        return Ok(ToLoginResponse(tokens));
    }

    /// <summary>Thu hồi refresh_token hiện có (nếu có) và xóa cả 2 cookie — idempotent, luôn trả 204.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshTokenRaw) && !string.IsNullOrEmpty(refreshTokenRaw))
        {
            await _authService.LogoutAsync(refreshTokenRaw, cancellationToken);
        }

        DeleteAuthCookies();

        return NoContent();
    }

    /// <summary>Cấp CSRF token ban đầu (cookie XSRF-TOKEN + body) — client gọi trước request POST/PUT đầu tiên.</summary>
    [HttpGet("csrf")]
    public IActionResult Csrf()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { csrfToken = tokens.RequestToken });
    }

    private void SetAuthCookies(AuthTokensResult tokens)
    {
        Response.Cookies.Append(AccessTokenCookieName, tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = tokens.AccessTokenExpiresAtUtc,
            Path = "/",
        });

        Response.Cookies.Append(RefreshTokenCookieName, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = tokens.RefreshTokenExpiresAtUtc,
            Path = RefreshTokenCookiePath,
        });
    }

    private void DeleteAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = RefreshTokenCookiePath });
    }

    private static LoginResponse ToLoginResponse(AuthTokensResult tokens) => new()
    {
        AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
        Username = tokens.Username,
        FullName = tokens.FullName,
        UserRole = tokens.UserRole,
    };
}
