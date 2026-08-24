namespace ProductionMES.Application.Services.PackingBoxes;

/// <summary>Dữ liệu cần merge vào mẫu tem (template) khi tạo tem dán thùng (US-25 AC4) — đúng tối thiểu các trường AC4 yêu cầu.</summary>
public class PackingLabelData
{
    public string Model { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public int PackingQuantity { get; set; }

    public decimal? GrossWeight { get; set; }

    public int BoxNo { get; set; }

    /// <summary>Ngày giờ đóng thùng (AC4) — giờ local nhà máy, đã format sẵn để in trực tiếp.</summary>
    public DateTime PackedAtLocal { get; set; }

    public string LineName { get; set; } = string.Empty;

    public string WorkStationName { get; set; } = string.Empty;
}
