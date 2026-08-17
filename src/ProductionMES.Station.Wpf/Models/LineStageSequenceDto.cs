namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>LineStageSequenceDto</c> phía backend — 1 công đoạn trong trình tự của Line (US-03).</summary>
public class LineStageSequenceDto
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public int StageId { get; set; }

    public int SequenceNumber { get; set; }

    public int? PreviousStageId { get; set; }
}
