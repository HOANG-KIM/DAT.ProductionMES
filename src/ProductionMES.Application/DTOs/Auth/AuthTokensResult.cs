using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Auth;

/// <summary>
/// Kênh truyền nội bộ giữa AuthService và AuthController (ADR-003) — chứa cả token thô để Controller đọc ra
/// set cookie. KHÔNG serialize DTO này thẳng ra JSON response (dùng <see cref="LoginResponse"/> cho việc đó),
/// vì <see cref="AccessToken"/>/<see cref="RefreshToken"/> không bao giờ được lộ ra JSON body.
/// </summary>
public class AuthTokensResult
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole UserRole { get; set; }

    /// <summary>
    /// Danh sách permission hiệu lực của <see cref="UserRole"/> tại thời điểm đăng nhập/refresh (ADR-004), dạng
    /// <c>"Resource.Action"</c> (vd. <c>"Line.View"</c>). Role không có permission nào (vd. <c>Operator</c>,
    /// <c>Manager</c> với dữ liệu seed hiện tại) trả về mảng rỗng, không lỗi.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}
