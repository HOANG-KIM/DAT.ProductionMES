namespace ProductionMES.Application.DTOs.Reports;

/// <summary>1 lượt scan OK đã cộng vào 1 thùng cụ thể (US-26/FR-26, AC7) — KHÔNG gồm lượt bị từ chối (xem <see cref="Services.Reports.IPackingProgressReportService.GetBoxScansAsync"/>).</summary>
public class PackingProgressReportBoxScanDto
{
    public string TagCode { get; set; } = string.Empty;

    public DateTime ScannedAtUtc { get; set; }
}
