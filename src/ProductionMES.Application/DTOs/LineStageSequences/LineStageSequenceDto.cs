namespace ProductionMES.Application.DTOs.LineStageSequences;

/// <summary>DTO trả về cho client, đại diện 1 công đoạn trong trình tự của 1 Line sản xuất (US-03/FR-03).</summary>
public class LineStageSequenceDto
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public int StageId { get; set; }

    public int SequenceNumber { get; set; }

    /// <summary>
    /// Id công đoạn liền trước trong cùng Line (suy ra từ SequenceNumber - 1) — null nếu đây là công đoạn đầu
    /// tiên của trình tự (FR-03/FR-08).
    /// </summary>
    public int? PreviousStageId { get; set; }
}
