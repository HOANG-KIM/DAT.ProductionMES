using ProductionMES.Application.Abstractions.Authorization;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Permissions;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Permissions;

/// <summary>
/// Implementation IPermissionService (ADR-004). Cấp/thu hồi permission ghi trực tiếp vào <see cref="RolePermission"/>
/// rồi gọi <see cref="IRolePermissionCache.Invalidate"/> ngay sau khi lưu DB thành công — để thay đổi có hiệu
/// lực ngay ở lần request tiếp theo, không chờ TTL cache hết hạn.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRolePermissionCache _rolePermissionCache;

    public PermissionService(IUnitOfWork unitOfWork, IRolePermissionCache rolePermissionCache)
    {
        _unitOfWork = unitOfWork;
        _rolePermissionCache = rolePermissionCache;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _unitOfWork.Repository<Permission>().GetAllAsync(cancellationToken);
        return permissions
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .Select(ToDto)
            .ToList();
    }

    public async Task<IReadOnlyList<RolePermissionMatrixDto>> GetRolePermissionMatrixAsync(CancellationToken cancellationToken = default)
    {
        var rolePermissions = await _unitOfWork.Repository<RolePermission>().GetAllAsync(cancellationToken);

        return Enum.GetValues<UserRole>()
            .Select(role => new RolePermissionMatrixDto
            {
                Role = role,
                PermissionIds = rolePermissions
                    .Where(rp => rp.Role == role)
                    .Select(rp => rp.PermissionId)
                    .OrderBy(id => id)
                    .ToList(),
            })
            .ToList();
    }

    public async Task GrantAsync(UserRole role, int permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _unitOfWork.Repository<Permission>().GetByIdAsync(permissionId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy Permission với Id = {permissionId}.");

        var rolePermissionRepository = _unitOfWork.Repository<RolePermission>();
        var existing = await rolePermissionRepository.FindAsync(
            rp => rp.Role == role && rp.PermissionId == permission.Id, cancellationToken);

        if (existing.Count > 0)
        {
            // Idempotent: role đã được cấp permission này từ trước — không tạo thêm bản ghi, không lỗi.
            return;
        }

        await rolePermissionRepository.AddAsync(new RolePermission { Role = role, PermissionId = permission.Id }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _rolePermissionCache.Invalidate();
    }

    public async Task RevokeAsync(UserRole role, int permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermissionRepository = _unitOfWork.Repository<RolePermission>();
        var existing = await rolePermissionRepository.FindAsync(
            rp => rp.Role == role && rp.PermissionId == permissionId, cancellationToken);

        if (existing.Count == 0)
        {
            // Idempotent: role chưa từng được cấp permission này — không lỗi.
            return;
        }

        foreach (var rolePermission in existing)
        {
            rolePermissionRepository.Remove(rolePermission);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _rolePermissionCache.Invalidate();
    }

    private static PermissionDto ToDto(Permission permission) => new()
    {
        Id = permission.Id,
        Resource = permission.Resource,
        Action = permission.Action,
    };
}
