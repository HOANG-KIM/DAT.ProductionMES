namespace ProductionMES.Domain.Enums;

/// <summary>
/// Resource quản lý bởi permission động (ADR-004). KHÔNG có <c>User</c> — API quản lý User
/// (<c>UsersController</c>) thuộc nhóm break-glass, giữ hardcode <c>[Authorize(Roles = "Admin")]</c>,
/// không đi qua hệ thống permission động này.
/// </summary>
public enum PermissionResource
{
    Line = 0,
    Stage = 1,
    WorkStation = 2,
    ProductionPlan = 3,
    ProductionPlanStage = 4,
}
