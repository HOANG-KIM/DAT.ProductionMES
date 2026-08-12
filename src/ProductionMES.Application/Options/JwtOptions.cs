namespace ProductionMES.Application.Options;

/// <summary>
/// Cấu hình JWT (đọc qua Options pattern, section "Jwt" trong appsettings) — dùng để phát hành token khi
/// đăng nhập (US-22). Phải khớp đúng Issuer/Audience/Key đã cấu hình cho middleware xác thực JWT ở
/// <c>Program.cs</c> (tầng Api).
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Thời hạn access token (phút, ADR-003). Mặc định 15 phút — ngắn để giảm cửa sổ rủi ro nếu token bị lộ
    /// qua kênh khác (vd. log, lỗi cấu hình); phiên đăng nhập dài hạn được duy trì qua refresh token
    /// (<see cref="RefreshTokenExpiryDays"/>), không phải bằng cách kéo dài access token.
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Thời hạn refresh token (ngày, ADR-003). Mặc định 7 ngày. Refresh token lưu server-side (entity
    /// <c>RefreshToken</c>, chỉ lưu hash) nên có thể thu hồi giữa chừng, khác với access token JWT thuần.
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
