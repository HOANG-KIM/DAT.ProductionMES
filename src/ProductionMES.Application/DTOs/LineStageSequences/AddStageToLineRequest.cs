namespace ProductionMES.Application.DTOs.LineStageSequences;

/// <summary>
/// Request thêm 1 công đoạn từ danh mục master vào trình tự của Line (AC1). Nếu không truyền
/// <see cref="SequenceNumber"/>, công đoạn được thêm vào cuối danh sách hiện tại theo trình tự mặc định (AC1).
/// </summary>
public class AddStageToLineRequest
{
    public int StageId { get; set; }

    public int? SequenceNumber { get; set; }
}
