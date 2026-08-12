namespace ProductionMES.Domain.Enums;

/// <summary>
/// Action quản lý bởi permission động (ADR-004). Không phải resource nào cũng dùng hết mọi action — mỗi
/// resource chỉ khớp đúng action thật đang tồn tại ở Controller tương ứng (xem catalog seed ở
/// <c>DbSeeder</c>): <c>Activate</c> chỉ có ở <c>ProductionPlan</c>; <c>Delete</c> chỉ có ở
/// <c>ProductionPlanStage</c> (HTTP DELETE thật, khác các resource còn lại dùng <c>Deactivate</c> — soft-delete).
/// </summary>
public enum PermissionAction
{
    View = 0,
    Create = 1,
    Update = 2,
    Activate = 3,
    Deactivate = 4,
    Delete = 5,
}
