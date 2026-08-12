using ProductionMES.Application.DTOs.Permissions;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Services.Permissions;

/// <summary>
/// Service quản lý catalog Permission và ma trận RolePermission (ADR-004) — phục vụ
/// <c>PermissionsController</c> (break-glass, hardcode <c>[Authorize(Roles = "Admin")]</c>).
/// </summary>
public interface IPermissionService
{
    /// <summary>Catalog toàn bộ Permission hợp lệ (cố định, không có API tạo/sửa/xóa).</summary>
    Task<IReadOnlyList<PermissionDto>> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Ma trận Role × Permission hiện tại — 1 dòng cho mỗi <see cref="UserRole"/>.</summary>
    Task<IReadOnlyList<RolePermissionMatrixDto>> GetRolePermissionMatrixAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cấp permission <paramref name="permissionId"/> cho <paramref name="role"/>. Idempotent — cấp lại
    /// permission đã có không lỗi, không tạo thêm bản ghi. Invalidate cache permission sau khi lưu DB thành công.
    /// </summary>
    Task GrantAsync(UserRole role, int permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thu hồi permission <paramref name="permissionId"/> khỏi <paramref name="role"/>. Idempotent — thu hồi
    /// permission chưa được cấp không lỗi. Invalidate cache permission sau khi lưu DB thành công.
    /// </summary>
    Task RevokeAsync(UserRole role, int permissionId, CancellationToken cancellationToken = default);
}
