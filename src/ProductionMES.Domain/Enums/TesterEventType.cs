namespace ProductionMES.Domain.Enums;

/// <summary>
/// Loại sự kiện máy thông điện ghi vào event log trên server (US-14 AC12, spec-app mục 8.2). Tên giá trị tương ứng
/// <c>VOID_REMOVED</c>, <c>VOID_DISCARD</c>, ... trong tài liệu.
/// </summary>
public enum TesterEventType
{
    /// <summary>VOID_REMOVED — dây bị rút khi đang giữ kết quả OK (US-12 AC5/AC6).</summary>
    VoidRemoved = 0,

    /// <summary>VOID_DISCARD — Tổ trưởng hủy kết quả đang chờ (US-12 AC11), kèm người duyệt.</summary>
    VoidDiscard = 1,

    /// <summary>VOID_REBOOT — máy reset (BOOT khác) làm mất kết quả đang chờ (US-14 AC5).</summary>
    VoidReboot = 2,

    /// <summary>VOID_LINK_LOST — máy tự hủy kết quả vì mất link quá lâu (US-14 AC5).</summary>
    VoidLinkLost = 3,

    /// <summary>LINK_LOST — mất kết nối máy (US-14 AC3).</summary>
    LinkLost = 4,

    /// <summary>LINK_RESTORED — kết nối máy có lại (US-14 AC4).</summary>
    LinkRestored = 5,

    /// <summary>BIND_FAIL — máy từ chối BIND hoặc không phản hồi (US-12 AC9/AC10).</summary>
    BindFail = 6,

    /// <summary>SCAN_ON_NG — quét QR khi dây NG còn trên jig (US-12 AC7), kèm mã tem.</summary>
    ScanOnNg = 7,

    /// <summary>DB_ERROR — lưu Scan lên server lỗi khi đang Saving (US-12 AC13).</summary>
    DbError = 8,
}
