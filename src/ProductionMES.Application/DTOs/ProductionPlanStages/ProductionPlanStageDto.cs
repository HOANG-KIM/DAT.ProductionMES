namespace ProductionMES.Application.DTOs.ProductionPlanStages;

/// <summary>DTO trả về cho client, đại diện 1 công đoạn trong trình tự của 1 kế hoạch sản xuất (US-03).</summary>
public class ProductionPlanStageDto
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int SequenceNumber { get; set; }

    /// <summary>
    /// Id công đoạn liền trước trong cùng kế hoạch (suy ra từ SequenceNumber - 1) — null nếu đây là công đoạn đầu tiên
    /// (FR-03/FR-08).
    /// </summary>
    public int? PreviousStageId { get; set; }
}
