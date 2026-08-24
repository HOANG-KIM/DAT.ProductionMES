namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ConfirmPackingDuplicateRequest</c> phía backend (US-25 AC8).</summary>
public class ConfirmPackingDuplicateRequest
{
    public int WorkStationId { get; set; }

    public string TagCode { get; set; } = string.Empty;

    public string? Note { get; set; }
}
