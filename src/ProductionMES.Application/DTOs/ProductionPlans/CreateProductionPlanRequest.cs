namespace ProductionMES.Application.DTOs.ProductionPlans;

/// <summary>
/// Request tạo mới 1 kế hoạch sản xuất (AC1 US-05). Kế hoạch tạo mới luôn ở trạng thái "Draft" một cách tự
/// nhiên (chưa có công đoạn nào được cấu hình/áp dụng — xem US-03/US-05a để cấu hình & áp dụng vào từng
/// công đoạn cụ thể).
/// </summary>
public class CreateProductionPlanRequest
{
    public int LineId { get; set; }

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
    /// là Lot HOÀN TOÀN MỚI (chưa từng có <c>ProductionPlan</c> nào trước đó) — Service tự kiểm tra qua
    /// <c>Services.Lots.ILotService.HasAnyProductionPlanAsync</c>, KHÔNG validate ở đây (cần truy vấn DB). Nếu Lot
    /// đã tồn tại, để <c>null</c> nghĩa là "giữ nguyên giá trị hiện có", không bắt buộc gửi lại.
    /// </summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>
    /// US-05 AC8 (=US-21a AC3) — xác nhận sửa "Tổng số lượng Lot" xuống dưới số đã chạy OK thực tế (soft-confirm).
    /// Dùng RIÊNG cho tình huống Lot đã tồn tại (VD tạo kế hoạch mới cho 1 Lot đã có lịch sử chạy ở Line khác) —
    /// mặc định <c>false</c>.
    /// </summary>
    public bool Confirm { get; set; }
}
