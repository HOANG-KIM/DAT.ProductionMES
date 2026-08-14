namespace ProductionMES.Domain.Enums;

/// <summary>
/// Vòng đời trạng thái của 1 cặp (Kế hoạch sản xuất, Công đoạn) — FR-05a/US-05a. KHÔNG phải cờ bật/tắt đơn giản
/// cho cả kế hoạch: mỗi công đoạn áp dụng kế hoạch có trạng thái riêng độc lập (lưu tại
/// <see cref="Entities.ProductionPlanStage.PlanStatus"/>), cho phép tạm dừng/chạy lại nhiều lần và đóng độc lập
/// mà không phải chờ các công đoạn khác của cùng kế hoạch hoàn thành.
/// </summary>
public enum PlanStatus
{
    /// <summary>Đã cấu hình áp dụng công đoạn nhưng chưa từng "Áp dụng" (chưa chạy lần nào) — US-05 AC1.</summary>
    Draft = 0,

    /// <summary>Đang active — cho phép ScanService ghi nhận lượt scan tính vào tiến độ (US-05a AC1).</summary>
    Running = 1,

    /// <summary>Tạm dừng, tiến độ vẫn giữ nguyên (tính động từ lịch sử scan) — có thể "Áp dụng" lại (US-05a AC3).</summary>
    Paused = 2,

    /// <summary>Đã đủ số lượng kế hoạch — tự động chuyển khi scan OK đủ số lượng (US-05a AC5).</summary>
    Completed = 3,

    /// <summary>Đóng sớm thủ công dù chưa đủ số lượng, yêu cầu xác nhận trước (US-05a AC6).</summary>
    Cancelled = 4,
}
