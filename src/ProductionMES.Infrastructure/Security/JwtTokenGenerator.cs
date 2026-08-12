using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductionMES.Application.Abstractions.Auth;
using ProductionMES.Application.Options;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Security;

/// <summary>
/// Implementation IJwtTokenGenerator (US-22, ADR-003) dựa trên System.IdentityModel.Tokens.Jwt cho access token
/// và System.Security.Cryptography cho refresh token. Đặt ở Infrastructure (không phải Application) vì đây là
/// chi tiết công nghệ cụ thể (ký/sinh token), cùng pattern với Repository/UnitOfWork — interface ở Application,
/// implementation ở Infrastructure.
/// Claim vai trò dùng type ngắn "role" (không dùng ClaimTypes.Role dài) và cấu hình khớp với
/// <c>TokenValidationParameters.RoleClaimType = "role"</c> ở Program.cs, để [Authorize(Roles = "...")] nhận
/// đúng vai trò mà không phụ thuộc cơ chế inbound claim type mapping mặc định (có thể khác nhau giữa
/// JwtSecurityTokenHandler và JsonWebTokenHandler).
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    public const string RoleClaimType = "role";

    /// <summary>Số byte entropy cho refresh token thô — 64 byte (512 bit) là dư an toàn cho token đối xứng dạng random.</summary>
    private const int RefreshTokenByteLength = 64;

    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(RoleClaimType, user.UserRole.ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));

    public string HashToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
