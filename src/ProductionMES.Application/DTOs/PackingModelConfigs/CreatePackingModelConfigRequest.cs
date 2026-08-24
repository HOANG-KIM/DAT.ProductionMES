namespace ProductionMES.Application.DTOs.PackingModelConfigs;

/// <summary>Request tạo mới 1 cấu hình Quy cách đóng gói cho 1 Model (AC1) — từ chối (409) nếu Model đã có cấu hình (so khớp AC9).</summary>
public class CreatePackingModelConfigRequest
{
    public string Model { get; set; } = string.Empty;

    public int PackingQuantity { get; set; }

    public decimal? GrossWeight { get; set; }

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }
}
