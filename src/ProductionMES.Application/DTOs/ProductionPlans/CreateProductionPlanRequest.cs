namespace ProductionMES.Application.DTOs.ProductionPlans;

/// <summary>
/// Request tạo mới 1 kế hoạch sản xuất (AC1). Kế hoạch tạo mới luôn ở trạng thái chưa active — kích hoạt
/// là 1 thao tác riêng (xem <see cref="Services.ProductionPlans.IProductionPlanService.ActivateAsync"/>).
/// </summary>
public class CreateProductionPlanRequest
{
    public int LineId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public string Shift { get; set; } = string.Empty;

    public DateTime EffectiveDate { get; set; }
}
