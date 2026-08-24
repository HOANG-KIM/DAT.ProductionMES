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

    /// <summary>US-25: true khi StageId là công đoạn "Đóng thùng" — các field Packing* bên dưới chỉ có giá trị khi cờ này true và Result = Ok.</summary>
    public bool IsPackingStage { get; set; }

    public int? PackingBoxNo { get; set; }

    public int? PackingScannedQuantity { get; set; }

    public int? PackingTargetQuantity { get; set; }

    /// <summary>AC4: true nếu ĐÚNG lượt scan này vừa làm đủ số lượng, hoàn tất 1 thùng.</summary>
    public bool PackingBoxCompleted { get; set; }

    /// <summary>Id thùng VỪA hoàn tất — dùng để tải tem in tự động (AC4/AC13).</summary>
    public int? PackingCompletedBoxId { get; set; }
}
