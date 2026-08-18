namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ReworkUnlockDto</c> phía backend (US-19 AC2) — kết quả 1 thao tác "Mở khóa rework".</summary>
public class ReworkUnlockDto
{
    public int Id { get; set; }

    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int UnlockedByUserId { get; set; }

    public string UnlockedByUserName { get; set; } = string.Empty;

    public DateTime UnlockedAtUtc { get; set; }

    public string? Note { get; set; }
}
