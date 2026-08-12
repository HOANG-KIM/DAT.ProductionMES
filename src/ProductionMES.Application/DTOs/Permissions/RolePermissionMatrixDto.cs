using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Permissions;

/// <summary>
/// Ma trận Role × Permission hiện tại (ADR-004) — 1 dòng cho mỗi <see cref="UserRole"/>, kèm danh sách Id
/// permission (khớp <see cref="PermissionDto.Id"/>) đang được cấp cho role đó. Dùng cho màn hình quản lý
/// permission ở <c>web-admin</c> (Admin bật/tắt qua UI).
/// </summary>
public class RolePermissionMatrixDto
{
    public UserRole Role { get; set; }

    public IReadOnlyList<int> PermissionIds { get; set; } = Array.Empty<int>();
}
