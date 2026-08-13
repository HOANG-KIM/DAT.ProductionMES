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

    /// <summary>Khung giờ nghỉ theo Line (US-01a) — cùng nhóm quản trị danh mục với Line, chỉ Admin thao tác.</summary>
    BreakWindow = 5,

    /// <summary>
    /// Quản lý API Key theo trạm (US-04a, ADR-005) — chỉ Admin thao tác (cấp/xem/thu hồi/cấp lại). Không liên
    /// quan tới permission dùng để xác thực <c>AuthenticationScheme "StationApiKey"</c> chính API key đó (đó là
    /// cơ chế xác thực riêng, không đi qua permission động — xem ADR-005 dòng 28).
    /// </summary>
    StationApiKey = 6,
}
