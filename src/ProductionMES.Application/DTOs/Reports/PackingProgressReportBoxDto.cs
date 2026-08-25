using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// 1 thùng (US-26/FR-26, AC6) — kết quả <see cref="Services.Reports.IPackingProgressReportService.GetBoxesAsync"/>.
/// Gồm CẢ thùng <see cref="PackingBoxStatus.Completed"/> lẫn <see cref="PackingBoxStatus.InProgress"/>, gộp theo
/// TẤT CẢ <see cref="Domain.Entities.ProductionPlan"/> cùng Lot tại cùng (Line, Công đoạn Đóng thùng) — cùng cách
/// gộp đã áp dụng cho <see cref="PackingProgressReportRowDto"/> (AC1/AC4), khác ở chỗ KHÔNG giới hạn
/// <see cref="PackingBoxStatus.Completed"/>.
/// </summary>
public class PackingProgressReportBoxDto
{
    /// <summary>Id thùng — dùng để drill-down xem chi tiết lượt scan (AC7, <c>GetBoxScansAsync</c>).</summary>
    public int Id { get; set; }

    public int BoxNo { get; set; }

    public PackingBoxStatus Status { get; set; }

    public int ScannedQuantity { get; set; }

    public int TargetQuantity { get; set; }

    public DateTime OpenedAtUtc { get; set; }

    /// <summary><c>null</c> khi <see cref="Status"/> = <see cref="PackingBoxStatus.InProgress"/>.</summary>
    public DateTime? CompletedAtUtc { get; set; }
}
