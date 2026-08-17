namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ProductionPlanDto</c> phía backend (US-05).</summary>
public class ProductionPlanDto
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    public decimal StandardQuantityPerHour { get; set; }
}
