using ProductionMES.Domain.Entities;

namespace ProductionMES.Application.Abstractions.Auth;

/// <summary>
/// Interface phát hành access token/refresh token cho tài khoản đăng nhập thành công (US-22, ADR-003). Đặt tại
/// tầng Application (không phải Infrastructure) để Service có thể phụ thuộc vào abstraction này mà không cần
/// reference ngược sang Infrastructure — implementation cụ thể (System.IdentityModel.Tokens.Jwt +
/// System.Security.Cryptography) nằm ở Infrastructure, cùng pattern với IRepository/IUnitOfWork.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>Sinh JWT (access token) kèm claim vai trò (role) cho <paramref name="user"/>, cùng thời điểm hết hạn (UTC).</summary>
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user);

    /// <summary>
    /// Sinh refresh token thô (chuỗi ngẫu nhiên đủ entropy, KHÔNG phải JWT) — chỉ Service lưu bản hash, giá trị
    /// thô trả về đây chỉ dùng 1 lần để set cookie, không lưu lại ở đâu khác.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>Hash 1 chuỗi token thô bằng SHA-256, trả về dạng hex string — dùng cả lúc lưu và lúc so khớp khi refresh/logout.</summary>
    string HashToken(string rawToken);
}
