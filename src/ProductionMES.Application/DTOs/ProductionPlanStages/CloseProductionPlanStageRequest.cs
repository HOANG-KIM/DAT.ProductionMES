namespace ProductionMES.Application.DTOs.ProductionPlanStages;

/// <summary>
/// Request đóng kế hoạch tại 1 công đoạn cụ thể (US-05a AC6) — "Đóng kế hoạch" đóng sớm thủ công, khác với
/// "Tạm dừng" (giữ tiến độ, có thể Áp dụng lại).
/// </summary>
public class CloseProductionPlanStageRequest
{
    /// <summary>
    /// Xác nhận đóng dù số lượng thực tế ("đã chạy") còn thấp hơn số lượng kế hoạch — mặc định <c>false</c>.
    /// Nếu số đã chạy chưa đủ và cờ này vẫn <c>false</c>, service từ chối kèm số lượng còn thiếu để client hiển
    /// thị popup xác nhận rồi gọi lại với <c>Confirm = true</c>.
    /// </summary>
    public bool Confirm { get; set; }
}
