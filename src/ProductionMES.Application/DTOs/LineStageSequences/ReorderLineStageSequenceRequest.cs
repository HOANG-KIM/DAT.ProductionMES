namespace ProductionMES.Application.DTOs.LineStageSequences;

/// <summary>
/// Request sắp xếp lại toàn bộ trình tự công đoạn của 1 Line (AC3) — nhập số thứ tự cho từng công đoạn
/// (kéo-thả ở tầng UI sẽ quy đổi về danh sách số thứ tự tương tự trước khi gọi API này).
/// </summary>
public class ReorderLineStageSequenceRequest
{
    public List<ReorderLineStageSequenceItem> Items { get; set; } = new();
}

public class ReorderLineStageSequenceItem
{
    public int StageId { get; set; }

    public int SequenceNumber { get; set; }
}
