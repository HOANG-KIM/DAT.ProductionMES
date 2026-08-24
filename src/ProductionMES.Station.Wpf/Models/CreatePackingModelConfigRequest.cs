namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>CreatePackingModelConfigRequest</c> phía backend (US-24 AC1).</summary>
public class CreatePackingModelConfigRequest
{
    public string Model { get; set; } = string.Empty;

    public int PackingQuantity { get; set; }

    public decimal? GrossWeight { get; set; }

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }
}
