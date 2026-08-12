namespace ProductionMES.Application.DTOs.Stages;

/// <summary>Request tạo mới 1 công đoạn master (AC1).</summary>
public class CreateStageRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
