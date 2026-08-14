using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Kết quả 1 lượt scan trả về cho client (US-07/US-08) — đủ dữ liệu để Station.Wpf (tương lai) hiển thị popup
/// OK/lỗi đúng theo FR-07 (mã tem, kết quả để chọn màu/âm thanh, lý do bị từ chối rõ ràng).
/// </summary>
public class ScanResultDto
{
    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int LineId { get; set; }

    public int WorkStationId { get; set; }

    public int ProductionPlanId { get; set; }

    /// <summary>Snapshot ProductionPlan.Customer tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Model tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Lot tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Revision tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string? Revision { get; set; }

    /// <summary>Snapshot ProductionPlan.PlannedQuantity tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>Snapshot ProductionPlan.TaktTimeSeconds tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public decimal TaktTimeSeconds { get; set; }

    public DateTime ScannedAtUtc { get; set; }

    public ScanResult Result { get; set; }

    /// <summary>Lý do bị từ chối (vd "Chưa qua công đoạn: Lắp ráp") — <c>null</c> khi <see cref="Result"/> = Ok.</summary>
    public string? RejectionReason { get; set; }
}
