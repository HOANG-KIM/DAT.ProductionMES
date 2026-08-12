using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProductionMES.Application.Abstractions.Auth;
using ProductionMES.Application.Abstractions.Authorization;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Auth;
using ProductionMES.Application.Options;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Application.Services.Auth;

/// <summary>
/// Implementation IAuthService (US-22, ADR-003). Băm/kiểm tra mật khẩu bằng
/// <see cref="IPasswordHasher{TUser}"/> (Microsoft.Extensions.Identity.Core — không cần cả hệ thống
/// ASP.NET Core Identity đầy đủ). Phát hành access token (JWT) + refresh token (opaque, lưu hash server-side để
/// thu hồi được) qua <see cref="IJwtTokenGenerator"/> (implementation ở Infrastructure). ADR-004: kèm theo
/// danh sách permission hiệu lực của role qua <see cref="IRolePermissionCache"/> (abstraction ở Application,
/// implementation cache ở Infrastructure — giữ đúng luật layer Application không reference Infrastructure).
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRolePermissionCache _rolePermissionCache;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRolePermissionCache rolePermissionCache,
        IOptions<JwtOptions> jwtOptions)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _rolePermissionCache = rolePermissionCache;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthTokensResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Repository<User>()
            .FindAsync(u => u.Username == request.Username, cancellationToken);
        var user = users.FirstOrDefault();

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokensResult?> RefreshAsync(string refreshTokenRaw, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenGenerator.HashToken(refreshTokenRaw);
        var refreshTokenRepository = _unitOfWork.Repository<RefreshToken>();

        var matches = await refreshTokenRepository.FindAsync(t => t.TokenHash == tokenHash, cancellationToken);
        var existingToken = matches.FirstOrDefault();

        if (existingToken is null)
        {
            return null;
        }

        var nowUtc = DateTime.UtcNow;

        if (existingToken.RevokedAtUtc is not null)
        {
            // Token đã bị thu hồi nhưng vẫn được gửi lên — dấu hiệu bị đánh cắp và dùng song song với chủ tài
            // khoản thật. Thu hồi toàn bộ refresh token đang hoạt động của user để buộc đăng nhập lại.
            var activeTokens = await refreshTokenRepository.FindAsync(
                t => t.UserId == existingToken.UserId && t.RevokedAtUtc == null && t.ExpiresAtUtc > nowUtc,
                cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAtUtc = nowUtc;
                refreshTokenRepository.Update(activeToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (existingToken.ExpiresAtUtc <= nowUtc)
        {
            return null;
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = await IssueTokensAsync(user, cancellationToken);

        existingToken.RevokedAtUtc = nowUtc;
        existingToken.ReplacedByTokenHash = _jwtTokenGenerator.HashToken(result.RefreshToken);
        refreshTokenRepository.Update(existingToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task LogoutAsync(string refreshTokenRaw, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenGenerator.HashToken(refreshTokenRaw);
        var refreshTokenRepository = _unitOfWork.Repository<RefreshToken>();

        var matches = await refreshTokenRepository.FindAsync(t => t.TokenHash == tokenHash, cancellationToken);
        var existingToken = matches.FirstOrDefault();

        if (existingToken is null)
        {
            return;
        }

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        refreshTokenRepository.Update(existingToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Sinh cặp access + refresh token mới cho <paramref name="user"/>, lưu bản ghi RefreshToken (chỉ lưu hash) và trả về kết quả đầy đủ.</summary>
    private async Task<AuthTokensResult> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenRaw = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtTokenGenerator.HashToken(refreshTokenRaw),
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ADR-004: danh sách permission hiệu lực của role tại thời điểm phát hành token — client dùng để chặn
        // UI (RouteGuard), không thay thế check permission ở backend.
        var permissions = await _rolePermissionCache.GetPermissionsAsync(user.UserRole, cancellationToken);

        return new AuthTokensResult
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            RefreshToken = refreshTokenRaw,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
            Username = user.Username,
            FullName = user.FullName,
            UserRole = user.UserRole,
            Permissions = permissions,
        };
    }
}
