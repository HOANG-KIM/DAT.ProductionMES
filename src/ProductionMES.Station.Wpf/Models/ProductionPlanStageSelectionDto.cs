namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ProductionPlanStageSelectionDto</c> phía backend (US-05b, GET /production-plan-stages).</summary>
public class ProductionPlanStageSelectionDto
{
    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int LineId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public decimal StandardQuantityPerHour { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    public PlanStatus PlanStatus { get; set; }

    public int RunCount { get; set; }

    public int RemainingCount { get; set; }
}
