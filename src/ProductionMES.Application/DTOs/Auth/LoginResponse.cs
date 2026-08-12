using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Auth;

/// <summary>
/// Kết quả đăng nhập/refresh thành công (US-22, ADR-003) — chỉ chứa thông tin hiển thị, KHÔNG chứa token
/// (token được set qua cookie HttpOnly ở Controller, không xuất hiện trong JSON body).
/// </summary>
public class LoginResponse
{
    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole UserRole { get; set; }

    /// <summary>
    /// Danh sách permission hiệu lực của <see cref="UserRole"/> (ADR-004), dạng <c>"Resource.Action"</c>
    /// (vd. <c>"Line.View"</c>) — <c>web-admin</c> dùng để chặn UI theo permission (RouteGuard), không thay thế
    /// check permission ở backend.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}
