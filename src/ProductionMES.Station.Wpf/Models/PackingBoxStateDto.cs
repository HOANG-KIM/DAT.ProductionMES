namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>PackingBoxStateDto</c> phía backend (US-25 AC5/AC6/AC9).</summary>
public class PackingBoxStateDto
{
    public bool RequiresStartingBoxNo { get; set; }

    public PackingBoxDto? CurrentBox { get; set; }

    public PackingBoxDto? LastCompletedBox { get; set; }
}
