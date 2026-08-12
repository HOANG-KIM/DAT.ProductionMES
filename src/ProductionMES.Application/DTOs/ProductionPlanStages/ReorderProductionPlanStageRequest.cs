namespace ProductionMES.Application.DTOs.ProductionPlanStages;

/// <summary>
/// Request sắp xếp lại toàn bộ trình tự công đoạn của 1 kế hoạch (AC3) — nhập số thứ tự cho từng công đoạn
/// (kéo-thả ở tầng UI sau này sẽ quy đổi về danh sách số thứ tự tương tự trước khi gọi API này).
/// </summary>
public class ReorderProductionPlanStageRequest
{
    public List<ReorderProductionPlanStageItem> Items { get; set; } = new();
}

public class ReorderProductionPlanStageItem
{
    public int StageId { get; set; }

    public int SequenceNumber { get; set; }
}
