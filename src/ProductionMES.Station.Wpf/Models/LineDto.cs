namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>LineDto</c> phía backend (US-01, danh mục Line sản xuất).</summary>
public class LineDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
