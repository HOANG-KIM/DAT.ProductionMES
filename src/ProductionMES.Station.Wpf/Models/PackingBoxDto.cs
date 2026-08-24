namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>PackingBoxDto</c> phía backend (US-25).</summary>
public class PackingBoxDto
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int WorkStationId { get; set; }

    public int BoxNo { get; set; }

    public PackingBoxStatus Status { get; set; }

    public int TargetQuantity { get; set; }

    public int ScannedQuantity { get; set; }

    public string Model { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public decimal? GrossWeight { get; set; }

    public DateTime OpenedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
