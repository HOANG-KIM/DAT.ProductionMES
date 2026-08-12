using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using ProductionMES.Application.Abstractions.Auth;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Auth;
using ProductionMES.Application.Options;
using ProductionMES.Application.Services.Auth;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho AuthService (US-22 — đăng nhập; ADR-003 — refresh/logout token).</summary>
public class AuthServiceTests
{
    private readonly Mock<IRepository<User>> _userRepositoryMock = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<RefreshToken>()).Returns(_refreshTokenRepositoryMock.Object);

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions { AccessTokenExpiryMinutes = 15, RefreshTokenExpiryDays = 7 });
        _sut = new AuthService(_unitOfWorkMock.Object, _passwordHasherMock.Object, _jwtTokenGeneratorMock.Object, jwtOptions);

        // Mặc định: hash trả về chuỗi cố định theo giá trị thô, đủ dùng để so khớp trong test (không cần SHA-256 thật).
        _jwtTokenGeneratorMock.Setup(g => g.HashToken(It.IsAny<string>())).Returns((string raw) => $"hash-{raw}");
    }

    private void SetupUserFindAsync(IReadOnlyList<User> users)
    {
        _userRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
    }

    private void SetupRefreshTokenFindAsync(IReadOnlyList<RefreshToken> tokens)
    {
        _refreshTokenRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<RefreshToken, bool>> predicate, CancellationToken _) =>
                tokens.Where(predicate.Compile()).ToList());
    }

    [Fact]
    public async Task LoginAsync_SaiTenDangNhap_TraVeNull()
    {
        SetupUserFindAsync(new List<User>());

        var result = await _sut.LoginAsync(new LoginRequest { Username = "khongtontai", Password = "abc" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_TaiKhoanBiVoHieuHoa_TraVeNull()
    {
        var user = new User { Id = 1, Username = "admin", PasswordHash = "hash", IsActive = false };
        SetupUserFindAsync(new List<User> { user });

        var result = await _sut.LoginAsync(new LoginRequest { Username = "admin", Password = "abc" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_SaiMatKhau_TraVeNull()
    {
        var user = new User { Id = 1, Username = "admin", PasswordHash = "hash", IsActive = true };
        SetupUserFindAsync(new List<User> { user });
        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "sai"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _sut.LoginAsync(new LoginRequest { Username = "admin", Password = "sai" });

        Assert.Null(result);
        _jwtTokenGeneratorMock.Verify(g => g.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_DungThongTin_TraVeTokenKemVaiTroVaLuuRefreshTokenMoi()
    {
        var user = new User { Id = 1, Username = "admin", PasswordHash = "hash", FullName = "Quản trị", UserRole = UserRole.Admin, IsActive = true };
        SetupUserFindAsync(new List<User> { user });
        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "dung"))
            .Returns(PasswordVerificationResult.Success);
        var accessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(15);
        _jwtTokenGeneratorMock.Setup(g => g.GenerateAccessToken(user)).Returns(("fake-access-token", accessTokenExpiresAtUtc));
        _jwtTokenGeneratorMock.Setup(g => g.GenerateRefreshToken()).Returns("fake-refresh-token");

        var result = await _sut.LoginAsync(new LoginRequest { Username = "admin", Password = "dung" });

        Assert.NotNull(result);
        Assert.Equal("fake-access-token", result!.AccessToken);
        Assert.Equal("fake-refresh-token", result.RefreshToken);
        Assert.Equal(UserRole.Admin, result.UserRole);
        Assert.Equal("admin", result.Username);
        _refreshTokenRepositoryMock.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(t => t.UserId == user.Id && t.TokenHash == "hash-fake-refresh-token"), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RefreshAsync_TokenHopLe_XoayVongDungCachVaTraVeKetQuaMoi()
    {
        var user = new User { Id = 1, Username = "admin", PasswordHash = "hash", FullName = "Quản trị", UserRole = UserRole.Admin, IsActive = true };
        var existingToken = new RefreshToken
        {
            Id = 10,
            UserId = user.Id,
            TokenHash = "hash-old-refresh-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            RevokedAtUtc = null,
        };
        SetupRefreshTokenFindAsync(new List<RefreshToken> { existingToken });
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _jwtTokenGeneratorMock.Setup(g => g.GenerateAccessToken(user)).Returns(("new-access-token", DateTime.UtcNow.AddMinutes(15)));
        _jwtTokenGeneratorMock.Setup(g => g.GenerateRefreshToken()).Returns("new-refresh-token");

        var result = await _sut.RefreshAsync("old-refresh-token");

        Assert.NotNull(result);
        Assert.Equal("new-access-token", result!.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.NotNull(existingToken.RevokedAtUtc);
        Assert.Equal("hash-new-refresh-token", existingToken.ReplacedByTokenHash);
        _refreshTokenRepositoryMock.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(t => t.TokenHash == "hash-new-refresh-token"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_TokenKhongTonTai_TraVeNull()
    {
        SetupRefreshTokenFindAsync(new List<RefreshToken>());

        var result = await _sut.RefreshAsync("khong-ton-tai");

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshAsync_TokenDaBiThuHoiTruocDo_ThuHoiToanBoTokenActiveCuaUserVaTraVeNull()
    {
        var revokedToken = new RefreshToken
        {
            Id = 1,
            UserId = 1,
            TokenHash = "hash-reused-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            RevokedAtUtc = DateTime.UtcNow.AddHours(-1),
        };
        var otherActiveToken = new RefreshToken
        {
            Id = 2,
            UserId = 1,
            TokenHash = "hash-another-active-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(2),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            RevokedAtUtc = null,
        };
        SetupRefreshTokenFindAsync(new List<RefreshToken> { revokedToken, otherActiveToken });

        var result = await _sut.RefreshAsync("reused-token");

        Assert.Null(result);
        Assert.NotNull(otherActiveToken.RevokedAtUtc);
        _refreshTokenRepositoryMock.Verify(r => r.Update(otherActiveToken), Times.Once);
        _jwtTokenGeneratorMock.Verify(g => g.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_TokenHetHan_TraVeNull()
    {
        var expiredToken = new RefreshToken
        {
            Id = 1,
            UserId = 1,
            TokenHash = "hash-expired-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-8),
            RevokedAtUtc = null,
        };
        SetupRefreshTokenFindAsync(new List<RefreshToken> { expiredToken });

        var result = await _sut.RefreshAsync("expired-token");

        Assert.Null(result);
        _jwtTokenGeneratorMock.Verify(g => g.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_UserDaBiKhoa_TraVeNull()
    {
        var user = new User { Id = 1, Username = "admin", PasswordHash = "hash", IsActive = false };
        var validToken = new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            TokenHash = "hash-valid-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            RevokedAtUtc = null,
        };
        SetupRefreshTokenFindAsync(new List<RefreshToken> { validToken });
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.RefreshAsync("valid-token");

        Assert.Null(result);
        _jwtTokenGeneratorMock.Verify(g => g.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_TokenTonTai_ThuHoiDungBanGhi()
    {
        var existingToken = new RefreshToken
        {
            Id = 1,
            UserId = 1,
            TokenHash = "hash-token-to-logout",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            RevokedAtUtc = null,
        };
        SetupRefreshTokenFindAsync(new List<RefreshToken> { existingToken });

        await _sut.LogoutAsync("token-to-logout");

        Assert.NotNull(existingToken.RevokedAtUtc);
        _refreshTokenRepositoryMock.Verify(r => r.Update(existingToken), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_TokenKhongTonTai_KhongThrowVaKhongLamGi()
    {
        SetupRefreshTokenFindAsync(new List<RefreshToken>());

        var exception = await Record.ExceptionAsync(() => _sut.LogoutAsync("khong-ton-tai"));

        Assert.Null(exception);
        _refreshTokenRepositoryMock.Verify(r => r.Update(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
