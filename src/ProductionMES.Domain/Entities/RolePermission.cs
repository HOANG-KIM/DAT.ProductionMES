using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// Permission nào đang được cấp cho role nào (ADR-004) — đây là bảng Admin chỉnh runtime qua UI
/// (<c>web-admin</c>, màn hình ma trận Role × Permission), không cần deploy lại code khi cần đổi quyền của
/// 1 role. Authorization động (<c>PermissionAuthorizationHandler</c>) tra cứu bảng này qua cache
/// (<c>IRolePermissionCache</c>), không query DB trực tiếp mỗi request.
/// </summary>
public class RolePermission
{
    public int Id { get; set; }

    public UserRole Role { get; set; }

    public int PermissionId { get; set; }

    public Permission? Permission { get; set; }
}
