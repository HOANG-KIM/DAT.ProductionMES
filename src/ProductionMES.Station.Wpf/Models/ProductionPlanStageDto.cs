namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ProductionPlanStageDto</c> phía backend — kết quả trả về từ Apply/Pause/Close (US-05a).</summary>
public class ProductionPlanStageDto
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int SequenceNumber { get; set; }

    public int? PreviousStageId { get; set; }

    public PlanStatus PlanStatus { get; set; }

    public int RunCount { get; set; }

    public int RemainingCount { get; set; }
}
