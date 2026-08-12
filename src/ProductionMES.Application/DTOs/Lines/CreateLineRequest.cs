namespace ProductionMES.Application.DTOs.Lines;

/// <summary>Request tạo mới 1 Line (AC1).</summary>
public class CreateLineRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
