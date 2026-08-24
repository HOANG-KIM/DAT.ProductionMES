namespace ProductionMES.Application.DTOs.PackingModelConfigs;

/// <summary>
/// Request sửa cấu hình đã có (AC2) — CHỈ sửa quy cách/khối lượng/tên sản phẩm/nhà sản xuất, KHÔNG đổi
/// <c>Model</c> (là khoá tra cứu — đổi Model coi như tạo cấu hình mới cho Model khác, không thuộc AC2).
/// </summary>
public class UpdatePackingModelConfigRequest
{
    public int PackingQuantity { get; set; }

    public decimal? GrossWeight { get; set; }

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }
}
