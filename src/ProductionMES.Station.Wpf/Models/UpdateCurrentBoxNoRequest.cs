namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>UpdateCurrentBoxNoRequest</c> phía backend (US-25 AC7).</summary>
public class UpdateCurrentBoxNoRequest
{
    public int WorkStationId { get; set; }

    public int NewBoxNo { get; set; }
}
