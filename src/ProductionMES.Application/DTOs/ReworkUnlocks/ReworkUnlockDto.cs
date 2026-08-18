namespace ProductionMES.Application.DTOs.ReworkUnlocks;

/// <summary>Kết quả 1 thao tác "Mở khóa rework" (US-19 AC2) trả về cho client.</summary>
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
