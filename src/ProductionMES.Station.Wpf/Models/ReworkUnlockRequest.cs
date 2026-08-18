namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>ReworkUnlockRequest</c> phía backend (US-19 AC2, request <c>POST api/v1/scans/rework-unlock</c>).
/// </summary>
/// <remarks>
/// <see cref="WorkStationId"/>: cùng cách <c>CreateNgScanRequest</c> (US-18) — endpoint dùng Bearer Tổ trưởng, nên
/// giá trị này là NGUỒN THẬT DUY NHẤT xác định trạm/công đoạn phía server, luôn gán từ
/// <c>StationOptions.WorkStationId</c> của chính trạm.
/// </remarks>
public class ReworkUnlockRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }

    public string? Note { get; set; }
}
