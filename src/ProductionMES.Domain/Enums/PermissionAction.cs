namespace ProductionMES.Domain.Enums;

/// <summary>
/// Action quản lý bởi permission động (ADR-004). Không phải resource nào cũng dùng hết mọi action — mỗi
/// resource chỉ khớp đúng action thật đang tồn tại ở Controller tương ứng (xem catalog seed ở
/// <c>DbSeeder</c>): <c>Delete</c> chỉ có ở <c>ProductionPlanStage</c> (HTTP DELETE thật, khác các resource
/// còn lại dùng <c>Deactivate</c> — soft-delete); <c>Apply</c>/<c>Pause</c>/<c>Close</c> chỉ có ở
/// <c>ProductionPlanStage</c> (US-05a — vòng đời trạng thái theo cặp Kế hoạch/Công đoạn, thay cho
/// <c>Activate</c>/<c>Deactivate</c> cấp cả kế hoạch trước đây); <c>ConfirmNg</c> chỉ có ở <c>Scan</c> (US-18,
/// thay đổi yêu cầu 18/08/2026 — bấm nút/quét mã "NG" bắt buộc đăng nhập Tổ trưởng có quyền này); <c>ReworkUnlock</c>
/// chỉ có ở <c>Scan</c> (US-19 — "Mở khóa rework" cho tem bị NG, chỉ Tổ trưởng/Admin).
/// </summary>
public enum PermissionAction
{
    View = 0,
    Create = 1,
    Update = 2,
    Activate = 3,
    Deactivate = 4,
    Delete = 5,

    /// <summary>US-05a AC1: chuyển 1 cặp (Kế hoạch, Công đoạn) sang Running.</summary>
    Apply = 6,

    /// <summary>US-05a AC3: chuyển 1 cặp (Kế hoạch, Công đoạn) từ Running sang Paused.</summary>
    Pause = 7,

    /// <summary>US-05a AC6: đóng sớm/kết thúc 1 cặp (Kế hoạch, Công đoạn), chuyển sang Cancelled.</summary>
    Close = 8,

    /// <summary>US-18 AC1/AC2/AC2a (thay đổi 18/08/2026): xác nhận Scan NG tại trạm — yêu cầu đăng nhập lại (re-auth) mỗi lần kích hoạt Chế độ Scan NG.</summary>
    ConfirmNg = 9,

    /// <summary>US-19 AC2/AC6: "Mở khóa rework" cho tem bị NG tại 1 công đoạn — chỉ Tổ trưởng/Admin.</summary>
    ReworkUnlock = 10,

    /// <summary>
    /// US-25 AC8: xác nhận đã biết tình huống tem trùng tại "Đóng thùng" — chỉ có ở <see cref="PermissionResource.PackingBox"/>.
    /// <b>SUPERSEDED bởi US-27 (25/08/2026)</b>: cơ chế audit riêng này bị thay thế hoàn toàn bởi
    /// <see cref="ConfirmReject"/> (áp dụng đồng nhất mọi <c>ScanResult</c> từ chối tự động, kể cả tem trùng tại
    /// Đóng thùng — xem US-27 AC12). Giữ nguyên định danh này (KHÔNG xóa/đổi số) chỉ để tương thích dữ liệu
    /// <c>Permission</c> đã seed từ trước 25/08/2026 — không còn Controller/Policy nào tham chiếu giá trị này nữa.
    /// </summary>
    ConfirmDuplicate = 11,

    /// <summary>
    /// US-27 AC5/AC6 (25/08/2026): xác nhận lưu 1 lượt scan bị hệ thống TỰ ĐỘNG từ chối (DuplicateTag/
    /// PreviousStageNotPassed/WaitingReworkUnlock/...) — chỉ có ở <see cref="PermissionResource.Scan"/>. Thay thế
    /// hoàn toàn <see cref="ConfirmDuplicate"/> cho công đoạn "Đóng thùng" (US-27 AC12) và áp dụng đồng nhất cho
    /// MỌI công đoạn khác (US-27 AC10) — trước đây các lượt này tự động lưu (FR-10 cũ), nay chỉ lưu sau khi Tổ
    /// trưởng/Admin/Manager đăng nhập xác nhận qua endpoint <c>POST api/v1/scans/reject-confirmations</c>.
    /// </summary>
    ConfirmReject = 12,
}
