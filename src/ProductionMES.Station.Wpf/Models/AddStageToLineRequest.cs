namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>AddStageToLineRequest</c> phía backend — thêm 1 công đoạn vào trình tự của Line (US-03 AC1).
/// Nếu không truyền <see cref="SequenceNumber"/>, công đoạn được thêm vào cuối danh sách hiện tại theo trình tự mặc định.
/// </summary>
public class AddStageToLineRequest
{
    public int StageId { get; set; }

    public int? SequenceNumber { get; set; }
}
