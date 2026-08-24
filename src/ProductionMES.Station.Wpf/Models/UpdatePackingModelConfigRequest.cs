namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>UpdatePackingModelConfigRequest</c> phía backend (US-24 AC2) — KHÔNG có Model.</summary>
public class UpdatePackingModelConfigRequest
{
    public int PackingQuantity { get; set; }

    public decimal? GrossWeight { get; set; }

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }
}
