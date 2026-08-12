using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Authorization;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Services.Permissions;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho PermissionService (ADR-004 — cấp/thu hồi permission cho role, idempotent).</summary>
public class PermissionServiceTests
{
    private readonly Mock<IRepository<Permission>> _permissionRepositoryMock = new();
    private readonly Mock<IRepository<RolePermission>> _rolePermissionRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IRolePermissionCache> _rolePermissionCacheMock = new();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Permission>()).Returns(_permissionRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<RolePermission>()).Returns(_rolePermissionRepositoryMock.Object);
        _sut = new PermissionService(_unitOfWorkMock.Object, _rolePermissionCacheMock.Object);
    }

    private void SetupRolePermissionFindAsync(IReadOnlyList<RolePermission> rolePermissions)
    {
        _rolePermissionRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RolePermission, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<RolePermission, bool>> predicate, CancellationToken _) =>
                rolePermissions.Where(predicate.Compile()).ToList());
    }

    [Fact]
    public async Task GrantAsync_PermissionChuaDuocCap_ThemMoiVaInvalidateCache()
    {
        var permission = new Permission { Id = 1, Resource = PermissionResource.Line, Action = PermissionAction.View };
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(permission);
        SetupRolePermissionFindAsync(new List<RolePermission>());

        await _sut.GrantAsync(UserRole.Supervisor, 1);

        _rolePermissionRepositoryMock.Verify(
            r => r.AddAsync(It.Is<RolePermission>(rp => rp.Role == UserRole.Supervisor && rp.PermissionId == 1), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _rolePermissionCacheMock.Verify(c => c.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task GrantAsync_PermissionDaDuocCapTruocDo_KhongThemMoiVaKhongLoi()
    {
        var permission = new Permission { Id = 1, Resource = PermissionResource.Line, Action = PermissionAction.View };
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(permission);
        SetupRolePermissionFindAsync(new List<RolePermission> { new() { Id = 10, Role = UserRole.Supervisor, PermissionId = 1 } });

        var exception = await Record.ExceptionAsync(() => _sut.GrantAsync(UserRole.Supervisor, 1));

        Assert.Null(exception);
        _rolePermissionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RolePermission>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _rolePermissionCacheMock.Verify(c => c.Invalidate(), Times.Never);
    }

    [Fact]
    public async Task GrantAsync_PermissionKhongTonTaiTrongCatalog_NemEntityNotFoundException()
    {
        _permissionRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Permission?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GrantAsync(UserRole.Admin, 99));

        _rolePermissionCacheMock.Verify(c => c.Invalidate(), Times.Never);
    }

    [Fact]
    public async Task RevokeAsync_PermissionDaDuocCap_XoaBanGhiVaInvalidateCache()
    {
        var existing = new RolePermission { Id = 10, Role = UserRole.Supervisor, PermissionId = 1 };
        SetupRolePermissionFindAsync(new List<RolePermission> { existing });

        await _sut.RevokeAsync(UserRole.Supervisor, 1);

        _rolePermissionRepositoryMock.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _rolePermissionCacheMock.Verify(c => c.Invalidate(), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_PermissionChuaTungDuocCap_KhongLoiVaKhongInvalidateCache()
    {
        SetupRolePermissionFindAsync(new List<RolePermission>());

        var exception = await Record.ExceptionAsync(() => _sut.RevokeAsync(UserRole.Supervisor, 1));

        Assert.Null(exception);
        _rolePermissionRepositoryMock.Verify(r => r.Remove(It.IsAny<RolePermission>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _rolePermissionCacheMock.Verify(c => c.Invalidate(), Times.Never);
    }
}
