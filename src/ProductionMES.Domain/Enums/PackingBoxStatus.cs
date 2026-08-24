namespace ProductionMES.Domain.Enums;

/// <summary>Trạng thái 1 thùng tại công đoạn "Đóng thùng" (US-25/FR-25).</summary>
public enum PackingBoxStatus
{
    /// <summary>Đang mở, chưa đủ số lượng theo Quy cách đóng gói đã snapshot (AC2/AC5/AC6).</summary>
    InProgress = 0,

    /// <summary>Đã đủ số lượng — tem thùng đã (được lệnh) in, thùng kế tiếp đã tự động mở (AC4).</summary>
    Completed = 1,
}
