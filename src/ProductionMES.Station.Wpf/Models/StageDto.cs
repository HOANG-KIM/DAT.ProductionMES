namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>StageDto</c> phía backend (US-02, danh mục Công đoạn master).</summary>
public class StageDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    /// <summary>US-25: true nếu đây là công đoạn "Đóng thùng" — mirror <c>Stage.IsPackingStage</c> phía backend, dùng để StationConfigViewModel tự suy ra <see cref="StationConfigViewModel.IsPackingStage"/> khi chọn trạm (tránh phải gõ tay JSON).</summary>
    public bool IsPackingStage { get; set; }
}
