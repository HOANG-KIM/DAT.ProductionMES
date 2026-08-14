namespace ProductionMES.Domain.Enums;

/// <summary>
/// Action quản lý bởi permission động (ADR-004). Không phải resource nào cũng dùng hết mọi action — mỗi
/// resource chỉ khớp đúng action thật đang tồn tại ở Controller tương ứng (xem catalog seed ở
/// <c>DbSeeder</c>): <c>Delete</c> chỉ có ở <c>ProductionPlanStage</c> (HTTP DELETE thật, khác các resource
/// còn lại dùng <c>Deactivate</c> — soft-delete); <c>Apply</c>/<c>Pause</c>/<c>Close</c> chỉ có ở
/// <c>ProductionPlanStage</c> (US-05a — vòng đời trạng thái theo cặp Kế hoạch/Công đoạn, thay cho
/// <c>Activate</c>/<c>Deactivate</c> cấp cả kế hoạch trước đây).
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
}
