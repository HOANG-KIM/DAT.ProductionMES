namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>CreateTesterScanRequest</c> phía backend (US-12 AC2) — <c>POST api/v1/scans/tester</c> (lưu Scan OK
/// gắn kết quả máy, idempotent theo BootId + Seq).
/// </summary>
public class CreateTesterScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }

    public string TesterBootId { get; set; } = string.Empty;

    public int TesterSeq { get; set; }

    public string GoldenCrc { get; set; } = string.Empty;

    public string FirmwareVersion { get; set; } = string.Empty;

    public DateTime TestedAt { get; set; }
}
