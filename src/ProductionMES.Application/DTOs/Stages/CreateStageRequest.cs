namespace ProductionMES.Application.DTOs.Stages;

/// <summary>Request tạo mới 1 công đoạn master (AC1).</summary>
public class CreateStageRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>US-25: đánh dấu đây là công đoạn "Đóng thùng" đặc thù — mặc định false.</summary>
    public bool IsPackingStage { get; set; }
}
