using ProductionMES.Application.DTOs.Auth;

namespace ProductionMES.Application.Services.Auth;

/// <summary>Service xác thực đăng nhập, phát hành/thu hồi access + refresh token (US-22, ADR-003).</summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng nhập bằng tên đăng nhập/mật khẩu. Trả về <c>null</c> nếu sai tên đăng nhập/mật khẩu hoặc tài khoản
    /// đã bị vô hiệu hóa — Controller ánh xạ sang HTTP 401 Unauthorized. Thành công thì lưu 1 bản ghi
    /// <c>RefreshToken</c> mới (chỉ lưu hash) và trả về <see cref="AuthTokensResult"/> chứa cả token thô để
    /// Controller set cookie.
    /// </summary>
    Task<AuthTokensResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đổi cặp access + refresh token dựa trên refresh token thô hiện có (rotation). Trả về <c>null</c> nếu
    /// token không tồn tại, đã hết hạn, hoặc tài khoản gắn với token đã bị vô hiệu hóa. Nếu token được gửi lên
    /// đã bị thu hồi từ trước (dấu hiệu bị đánh cắp/reuse), thu hồi toàn bộ refresh token đang hoạt động của
    /// user đó rồi trả <c>null</c> — buộc đăng nhập lại.
    /// </summary>
    Task<AuthTokensResult?> RefreshAsync(string refreshTokenRaw, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thu hồi refresh token thô hiện có (nếu tồn tại). Idempotent/an toàn — không throw kể cả khi token không
    /// tồn tại (cookie rác hoặc đã bị xóa từ trước), vì logout luôn phải thành công từ góc nhìn người dùng.
    /// </summary>
    Task LogoutAsync(string refreshTokenRaw, CancellationToken cancellationToken = default);
}
