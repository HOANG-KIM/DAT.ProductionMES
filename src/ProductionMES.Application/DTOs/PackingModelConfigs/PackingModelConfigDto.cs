namespace ProductionMES.Application.DTOs.PackingModelConfigs;

/// <summary>DTO trả về cho client, đại diện 1 cấu hình Quy cách đóng gói theo Model (US-24/FR-24).</summary>
public class PackingModelConfigDto
{
    public int Id { get; set; }

    public string Model { get; set; } = string.Empty;

    public int PackingQuantity { get; set; }

    public decimal? GrossWeight { get; set; }

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    /// <summary>true nếu đã có file mẫu tem (template .xlsx) được tải lên (AC3).</summary>
    public bool HasTemplate { get; set; }

    public DateTime? TemplateUpdatedAtUtc { get; set; }

    public string? TemplateUpdatedByUserName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? UpdatedByUserName { get; set; }
}
