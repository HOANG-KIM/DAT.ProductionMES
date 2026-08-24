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

    /// <summary>
    /// Cấu hình Quy cách đóng gói theo Model (US-24/FR-24, công đoạn Đóng thùng) — dùng <c>View</c>/<c>Create</c>/
    /// <c>Update</c> (không có <c>Delete</c>/<c>Deactivate</c> — chưa có AC nào yêu cầu xoá/vô hiệu hóa cấu hình).
    /// Quản lý được từ CẢ web-admin (Admin) lẫn Station.Wpf (Tổ trưởng nâng quyền tại trạm) — cấp cho cả 2 role
    /// (AC6), khác các resource danh mục khác trước đây chỉ Admin.
    /// </summary>
    PackingModelConfig = 9,

    /// <summary>
    /// Thao tác trên thùng tại công đoạn "Đóng thùng" (US-25/FR-25) CẦN đăng nhập Supervisor (tái sử dụng cơ chế
    /// re-auth mỗi lần của US-18): <c>Update</c> — sửa số thùng hiện tại (AC7); <c>ConfirmDuplicate</c> — xác
    /// nhận đã biết tình huống tem trùng, chỉ audit, không cộng số lượng (AC8). Đọc trạng thái thùng hiện tại
    /// (GetState) và nhập số thùng bắt đầu (AC5) KHÔNG đi qua permission này — dùng scheme <c>StationApiKey</c>
    /// như luồng scan chuẩn (Operator không đăng nhập cá nhân, xem <see cref="Scan"/>).
    /// </summary>
    PackingBox = 10,
}
