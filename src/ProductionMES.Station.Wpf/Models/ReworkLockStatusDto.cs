namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>ReworkLockStatusDto</c> phía backend — thông tin lỗi NG gần nhất + trạng thái khóa rework
/// hiện tại của 1 tem tại đúng công đoạn của trạm này (US-19, feedback 18/08/2026).
/// </summary>
public class ReworkLockStatusDto
{
    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public bool IsLocked { get; set; }

    public bool HasNgHistory { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? NgScannedAtUtc { get; set; }

    public string? NgConfirmedByUserName { get; set; }

    public int NgCount { get; set; }
}
