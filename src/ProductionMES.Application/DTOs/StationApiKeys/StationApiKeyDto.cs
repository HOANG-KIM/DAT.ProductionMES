namespace ProductionMES.Application.DTOs.StationApiKeys;

/// <summary>
/// DTO metadata của API Key hiện tại 1 trạm (US-04a AC2) — KHÔNG chứa <c>KeyHash</c> lẫn giá trị thô, chỉ dùng
/// để hiển thị trạng thái Active/Revoked + ngày cấp trên màn hình quản lý trạm.
/// </summary>
public class StationApiKeyDto
{
    public int Id { get; set; }

    public int WorkStationId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Active khi chưa bị thu hồi (<see cref="RevokedAtUtc"/> null).</summary>
    public bool IsActive => RevokedAtUtc is null;
}
