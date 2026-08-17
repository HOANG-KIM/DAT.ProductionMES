namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>ScanResultDto</c> phía backend (US-07/US-08, response <c>POST api/v1/scans</c> và payload
/// SignalR <c>ScanRecorded</c>) — chỉ khai báo các field UI hiện dùng, đủ để deserialize (System.Text.Json bỏ
/// qua field lạ không khai báo).
/// </summary>
public class ScanResultDto
{
    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int LineId { get; set; }

    public int WorkStationId { get; set; }

    public int ProductionPlanId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public DateTime ScannedAtUtc { get; set; }

    public ScanResult Result { get; set; }

    /// <summary>Lý do bị từ chối (vd "Chưa qua công đoạn: Lắp ráp") — <c>null</c> khi <see cref="Result"/> = Ok.</summary>
    public string? RejectionReason { get; set; }
}
