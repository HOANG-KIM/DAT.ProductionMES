namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>StageDto</c> phía backend (US-02, danh mục Công đoạn master).</summary>
public class StageDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
