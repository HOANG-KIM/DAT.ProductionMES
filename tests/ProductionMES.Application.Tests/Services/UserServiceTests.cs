using Microsoft.AspNetCore.Identity;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Users;
using ProductionMES.Application.Services.Users;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho UserService, bám theo AC1 của US-22 (Documents/BACKLOG-user-story.md).</summary>
public class UserServiceTests
{
    private readonly Mock<IRepository<User>> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<User>()).Returns(_repositoryMock.Object);
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>())).Returns("hashed-password");
        _sut = new UserService(_unitOfWorkMock.Object, _passwordHasherMock.Object);
    }

    // AC1 — Admin gán vai trò cho tài khoản khi tạo mới.
    [Fact]
    public async Task CreateAsync_TenDangNhapChuaTonTai_TaoThanhCongVaBamMatKhau()
    {
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var request = new CreateUserRequest { Username = "totruong1", Password = "MatKhau123", FullName = "Nguyễn Văn A", UserRole = UserRole.Supervisor };

        var result = await _sut.CreateAsync(request);

        Assert.Equal("totruong1", result.Username);
        Assert.Equal(UserRole.Supervisor, result.UserRole);
        Assert.True(result.IsActive);
        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<User>(), "MatKhau123"), Times.Once);
        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<User>(u => u.PasswordHash == "hashed-password"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_TrungTenDangNhap_NemBusinessRuleException()
    {
        var existing = new User { Id = 1, Username = "admin" };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existing });

        var request = new CreateUserRequest { Username = "admin", Password = "abc", FullName = "X", UserRole = UserRole.Operator };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request));
    }

    // AC1 — Admin gán/đổi vai trò cho tài khoản đã tồn tại.
    [Fact]
    public async Task UpdateVaiTroAsync_TaiKhoanTonTai_CapNhatVaiTroThanhCong()
    {
        var existing = new User { Id = 1, Username = "cn1", UserRole = UserRole.Operator, IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateUserRoleAsync(1, new UpdateUserRoleRequest { UserRole = UserRole.Supervisor });

        Assert.Equal(UserRole.Supervisor, result.UserRole);
        _repositoryMock.Verify(r => r.Update(existing), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_TaiKhoanDangHoatDong_ChuyenTrangThaiNgungHoatDongVaKhongXoaCung()
    {
        var existing = new User { Id = 1, Username = "cn1", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeactivateAsync(1);

        Assert.False(existing.IsActive);
        _repositoryMock.Verify(r => r.Remove(It.IsAny<User>()), Times.Never);
    }

    // AC3 — Không có vai trò "QC" riêng biệt: hệ thống chỉ định nghĩa đúng 4 vai trò theo mục 2.2 SRS.
    [Fact]
    public void VaiTroEnum_ChiCoDung4VaiTro_KhongCoQCRieng()
    {
        var values = Enum.GetValues<UserRole>();

        Assert.Equal(4, values.Length);
        Assert.Contains(UserRole.Operator, values);
        Assert.Contains(UserRole.Supervisor, values);
        Assert.Contains(UserRole.Admin, values);
        Assert.Contains(UserRole.Manager, values);
    }
}
