namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>PackingBoxStateDto</c> phía backend (US-25 AC5/AC6/AC9).</summary>
public class PackingBoxStateDto
{
    public bool RequiresStartingBoxNo { get; set; }

    public PackingBoxDto? CurrentBox { get; set; }

    public PackingBoxDto? LastCompletedBox { get; set; }

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    /// <summary>Số thùng gợi ý (số lớn nhất đã dùng của cùng Model + Lot + 1) — điền sẵn khi nhập/sửa số thùng.</summary>
    public int SuggestedNextBoxNo { get; set; } = 1;
}
