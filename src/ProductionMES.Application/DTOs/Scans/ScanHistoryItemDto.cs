using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// 1 dòng kết quả tra cứu lịch sử scan (US-10 AC2/AC3). Giữ đủ field snapshot bất biến
/// (<see cref="Customer"/>/<see cref="Model"/>/<see cref="Lot"/>/<see cref="Revision"/>/
/// <see cref="PlannedQuantity"/>/<see cref="TaktTimeSeconds"/>) đúng như đã lưu tại thời điểm scan — KHÔNG tra
/// cứu động qua <see cref="ProductionPlanId"/> (AC4, xem remarks tại entity <c>Scan</c>).
/// </summary>
public class ScanHistoryItemDto
{
    public int Id { get; set; }

    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int LineId { get; set; }

    public int WorkStationId { get; set; }

    public int ProductionPlanId { get; set; }

    /// <summary>Snapshot ProductionPlan.Customer tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Model tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Lot tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Revision tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string? Revision { get; set; }

    /// <summary>Snapshot ProductionPlan.PlannedQuantity tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>Snapshot ProductionPlan.TaktTimeSeconds tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public decimal TaktTimeSeconds { get; set; }

    public DateTime ScannedAtUtc { get; set; }

    public ScanResult Result { get; set; }

    public string? RejectionReason { get; set; }
}
