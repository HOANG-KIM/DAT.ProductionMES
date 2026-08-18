namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>CreateNgScanRequest</c> phía backend (US-18 AC3/AC5, request <c>POST api/v1/scans/ng</c>).
/// </summary>
/// <remarks>
/// <see cref="WorkStationId"/> (thay đổi 18/08/2026): endpoint này đổi sang Bearer Tổ trưởng (không còn
/// StationApiKey), nên giá trị này là NGUỒN THẬT DUY NHẤT xác định trạm phía server — luôn gán từ
/// <c>StationOptions.WorkStationId</c> của chính trạm, KHÔNG cho người dùng gõ tay.
/// </remarks>
public class CreateNgScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }

    public string RejectionReason { get; set; } = string.Empty;
}
