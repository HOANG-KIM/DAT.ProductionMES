namespace ProductionMES.Application.DTOs.Stages;

/// <summary>DTO trả về cho client, đại diện 1 công đoạn (danh mục master) (US-02).</summary>
public class StageDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
