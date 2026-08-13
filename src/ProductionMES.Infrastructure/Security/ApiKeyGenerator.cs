using System.Security.Cryptography;
using System.Text;
using ProductionMES.Application.Abstractions.Security;

namespace ProductionMES.Infrastructure.Security;

/// <summary>
/// Implementation IApiKeyGenerator (US-04a, ADR-005) — cùng thuật toán (SHA-256 hex, random byte đủ dài) đã
/// dùng cho <see cref="JwtTokenGenerator"/> khi sinh/hash refresh token, theo đúng nguyên tắc ADR-005 đã chốt
/// ("cùng nguyên tắc RefreshToken.TokenHash — ADR-003/ADR-005").
/// </summary>
public class ApiKeyGenerator : IApiKeyGenerator
{
    /// <summary>Số byte entropy cho API key thô — 64 byte (512 bit), cùng độ dài đã dùng cho refresh token.</summary>
    private const int ApiKeyByteLength = 64;

    public string GenerateApiKey()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(ApiKeyByteLength));

    public string HashApiKey(string rawApiKey)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawApiKey));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
