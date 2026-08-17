using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Auth;

/// <summary>
/// Kết quả đăng nhập/refresh cho luồng Supervisor tại <c>Station.Wpf</c> (ADR-005) — KHÁC <see cref="LoginResponse"/>
/// dùng cho <c>web-admin</c>: token trả thẳng trong JSON body (không set cookie), vì desktop client tự lưu
/// access token trong bộ nhớ tiến trình và tự gắn header <c>Authorization: Bearer</c>.
/// </summary>
public class StationLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole UserRole { get; set; }

    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}
