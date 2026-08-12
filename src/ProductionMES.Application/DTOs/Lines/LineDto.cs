namespace ProductionMES.Application.DTOs.Lines;

/// <summary>DTO trả về cho client, đại diện 1 Line sản xuất (không lộ EF entity ra ngoài Application).</summary>
public class LineDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
