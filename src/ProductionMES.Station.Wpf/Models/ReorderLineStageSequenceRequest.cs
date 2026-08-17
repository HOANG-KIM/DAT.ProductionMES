namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ReorderLineStageSequenceRequest</c> phía backend (US-03 AC3).</summary>
public class ReorderLineStageSequenceRequest
{
    public List<ReorderLineStageSequenceItem> Items { get; set; } = new();
}

/// <summary>Mirror JSON của <c>ReorderLineStageSequenceItem</c> phía backend.</summary>
public class ReorderLineStageSequenceItem
{
    public int StageId { get; set; }

    public int SequenceNumber { get; set; }
}
