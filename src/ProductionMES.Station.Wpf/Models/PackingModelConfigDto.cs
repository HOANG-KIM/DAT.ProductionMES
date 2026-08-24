namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>PackingModelConfigDto</c> phía backend (US-24) — cấu hình Quy cách đóng gói theo Model.</summary>
public class PackingModelConfigDto
{
    public int Id { get; set; }

    public string Model { get; set; } = string.Empty;

    public int PackingQuantity { get; set; }

    public decimal? GrossWeight { get; set; }

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public bool HasTemplate { get; set; }

    public DateTime? TemplateUpdatedAtUtc { get; set; }

    public string? TemplateUpdatedByUserName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? UpdatedByUserName { get; set; }
}
