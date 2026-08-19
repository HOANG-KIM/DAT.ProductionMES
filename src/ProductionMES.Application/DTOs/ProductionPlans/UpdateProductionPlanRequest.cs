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
    /// US-05 AC7 (=US-21a AC1) — "Tổng số lượng Lot", nhập tay. BẮT BUỘC (khác <c>null</c>) khi <see cref="Lot"/>
    /// (sau khi sửa) là Lot HOÀN TOÀN MỚI (chưa từng có <c>ProductionPlan</c> nào khác trước đó). Để <c>null</c>
    /// nghĩa là "giữ nguyên giá trị hiện có" — không bắt buộc gửi lại mỗi lần sửa kế hoạch.
    /// </summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>
    /// AC5: kế hoạch đã có ít nhất 1 công đoạn đang Running/Paused mà sửa Số lượng kế hoạch hoặc Takt time là
    /// tình huống "chạy dở, sửa nhầm có thể sai lệch tiến độ". Mặc định <c>false</c> — nếu service phát hiện
    /// tình huống này và cờ này vẫn <c>false</c>, sẽ từ chối kèm cảnh báo rõ ràng (không phải lỗi cấu hình) để
    /// client hiển thị popup xác nhận rồi gọi lại với <c>Confirm = true</c>.
    /// Dùng CHUNG cho cả AC8 (US-21a AC3 — sửa "Tổng số lượng Lot" xuống dưới số đã chạy OK thực tế).
    /// </summary>
    public bool Confirm { get; set; }
}
