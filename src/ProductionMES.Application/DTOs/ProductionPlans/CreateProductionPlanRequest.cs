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
}
