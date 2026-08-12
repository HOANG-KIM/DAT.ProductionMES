namespace ProductionMES.Application.DTOs.ProductionPlans;

/// <summary>Request cập nhật thông tin 1 kế hoạch đã tồn tại (AC3). Không đổi trạng thái active qua endpoint này.</summary>
public class UpdateProductionPlanRequest
{
    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public string Shift { get; set; } = string.Empty;

    public DateTime EffectiveDate { get; set; }
}
