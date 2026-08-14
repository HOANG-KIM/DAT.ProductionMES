namespace ProductionMES.Application.DTOs.ProductionPlans;

/// <summary>Request cập nhật thông tin 1 kế hoạch đã tồn tại (AC4/AC5 US-05). Không đổi LineId qua endpoint này.</summary>
public class UpdateProductionPlanRequest
{
    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    /// <summary>Có thể để trống, không bắt buộc (AC2).</summary>
    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    /// <summary>
    /// AC5: kế hoạch đã có ít nhất 1 công đoạn đang Running/Paused mà sửa Số lượng kế hoạch hoặc Takt time là
    /// tình huống "chạy dở, sửa nhầm có thể sai lệch tiến độ". Mặc định <c>false</c> — nếu service phát hiện
    /// tình huống này và cờ này vẫn <c>false</c>, sẽ từ chối kèm cảnh báo rõ ràng (không phải lỗi cấu hình) để
    /// client hiển thị popup xác nhận rồi gọi lại với <c>Confirm = true</c>.
    /// </summary>
    public bool Confirm { get; set; }
}
