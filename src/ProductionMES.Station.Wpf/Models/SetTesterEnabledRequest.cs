namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>SetTesterEnabledRequest</c> phía backend (US-11 AC4) — <c>PUT api/v1/tester/enabled</c>.</summary>
public class SetTesterEnabledRequest
{
    public int WorkStationId { get; set; }

    public bool Enabled { get; set; }

    public string Reason { get; set; } = string.Empty;
}
