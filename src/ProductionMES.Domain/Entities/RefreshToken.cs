namespace ProductionMES.Domain.Entities;

/// <summary>
/// Refresh token dài hạn (ADR-003), lưu server-side để có thể thu hồi giữa chừng — điều JWT thuần (chỉ có
/// access token) không làm được (vd. khi Admin khóa tài khoản, khi phát hiện token bị lộ).
/// </summary>
/// <remarks>
/// <see cref="TokenHash"/> lưu SHA-256 (dạng hex) của giá trị thô, KHÔNG BAO GIỜ lưu giá trị thô — nếu DB bị
/// lộ (backup rò rỉ, SQL injection...), kẻ tấn công không có refresh token dùng được ngay, cùng nguyên tắc với
/// <see cref="User.PasswordHash"/> không lưu mật khẩu plaintext.
/// </remarks>
public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>SHA-256 (hex) của refresh token thô — dùng để tìm/so khớp khi refresh/logout.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Thời điểm bị thu hồi (UTC), <c>null</c> nếu còn hoạt động. Bị set khi rotate (refresh thành công),
    /// khi logout, hoặc khi phát hiện reuse (token đã revoke nhưng vẫn được gửi lên — dấu hiệu bị đánh cắp).
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Hash của refresh token đã thay thế token này khi rotate — phục vụ truy vết chuỗi rotation (không dùng
    /// để xác thực, chỉ để điều tra/audit khi cần).
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }
}
