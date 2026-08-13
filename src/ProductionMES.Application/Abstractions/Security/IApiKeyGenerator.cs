namespace ProductionMES.Application.Abstractions.Security;

/// <summary>
/// Sinh/hash API Key theo trạm (US-04a, ADR-005). Đặt tại tầng Application (không phải Infrastructure) để
/// Service có thể phụ thuộc vào abstraction này mà không cần reference ngược sang Infrastructure — cùng pattern
/// với <c>IJwtTokenGenerator</c>. Tách riêng interface khỏi <c>IJwtTokenGenerator</c> dù dùng chung thuật toán
/// (SHA-256, random byte đủ dài — ADR-005: "cùng nguyên tắc RefreshToken.TokenHash") vì API Key trạm không gắn
/// với khái niệm access/refresh token của người dùng (không có <c>User</c> nào đứng sau request dùng scheme này).
/// </summary>
public interface IApiKeyGenerator
{
    /// <summary>Sinh API key thô (chuỗi ngẫu nhiên đủ entropy) — chỉ Service lưu bản hash, giá trị thô trả về
    /// đúng 1 lần cho Admin sao chép vào file cấu hình cục bộ của trạm (AC1/AC4).</summary>
    string GenerateApiKey();

    /// <summary>Hash 1 chuỗi API key thô bằng SHA-256, trả về dạng hex string — dùng cả lúc lưu và lúc so khớp khi xác thực.</summary>
    string HashApiKey(string rawApiKey);
}
