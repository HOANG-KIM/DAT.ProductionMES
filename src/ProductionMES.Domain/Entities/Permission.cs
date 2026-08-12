using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// Catalog cố định các permission hợp lệ trong hệ thống (ADR-004) — mỗi bản ghi là 1 cặp
/// (<see cref="Resource"/>, <see cref="Action"/>) khớp đúng action thật đang tồn tại ở Controller tương ứng.
/// KHÔNG phải tích chéo đầy đủ Resource x Action (vd. <c>Line</c> không có <c>Delete</c> vì API chỉ có
/// <c>deactivate</c>, không xóa cứng) — xem seed đầy đủ ở <c>DbSeeder</c>. Bảng này không có API xóa/sửa,
/// chỉ <c>RolePermission</c> (cấp/thu hồi permission cho role) mới được Admin chỉnh qua UI runtime.
/// </summary>
public class Permission
{
    public int Id { get; set; }

    public PermissionResource Resource { get; set; }

    public PermissionAction Action { get; set; }
}
