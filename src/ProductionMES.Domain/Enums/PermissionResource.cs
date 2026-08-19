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

    /// <summary>
    /// <c>View</c>: tra cứu lịch sử scan (US-10 AC2/AC3) — Tổ trưởng (Supervisor)/Admin/Ban quản lý (Manager)
    /// thao tác, KHÔNG dùng scheme <c>StationApiKey</c> (khác endpoint ghi scan <c>ScansController.Create</c> —
    /// xem ADR-005). <c>ConfirmNg</c> (US-18, thay đổi 18/08/2026): xác nhận Scan NG tại trạm, cũng dùng scheme
    /// Bearer mặc định (KHÔNG dùng <c>StationApiKey</c>) — yêu cầu đăng nhập lại (re-auth) mỗi lần. <c>ReworkUnlock</c>
    /// (US-19): "Mở khóa rework" cho tem bị NG — Bearer mặc định, chỉ Tổ trưởng/Admin (KHÔNG cấp Manager, khác
    /// <c>ConfirmNg</c>/<c>View</c> — xem AC6 US-19).
    /// </summary>
    Scan = 7,

    /// <summary>
    /// Báo cáo tổng hợp theo Line/công đoạn (US-21, FR-21) — hiện chỉ dùng <c>View</c>. Tách riêng khỏi
    /// <see cref="Scan"/> dù cùng đọc dữ liệu Scan bên trong, vì đây là góc nhìn tổng hợp (Ban quản lý/Văn phòng)
    /// khác với tra cứu chi tiết lịch sử theo tem — cho phép Admin cấp/thu hồi 2 quyền độc lập nhau qua UI phân
    /// quyền (ADR-004) sau này nếu cần.
    /// </summary>
    Report = 8,
}
