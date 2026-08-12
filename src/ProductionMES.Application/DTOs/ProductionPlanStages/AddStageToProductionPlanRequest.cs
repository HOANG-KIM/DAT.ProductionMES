namespace ProductionMES.Application.DTOs.ProductionPlanStages;

/// <summary>
/// Request thêm 1 công đoạn từ danh mục master vào kế hoạch (AC1). Nếu không truyền <see cref="SequenceNumber"/>,
/// công đoạn được thêm vào cuối danh sách hiện tại theo trình tự mặc định (AC1).
/// </summary>
public class AddStageToProductionPlanRequest
{
    public int StageId { get; set; }

    public int? SequenceNumber { get; set; }
}
