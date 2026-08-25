namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// Kết quả xem chi tiết lượt scan của 1 thùng (US-26/FR-26, AC7/AC8).
/// </summary>
public class PackingProgressReportBoxScansDto
{
    /// <summary>
    /// AC8: <c>false</c> khi thùng có <see cref="Domain.Entities.PackingBox.ScannedQuantity"/> > 0 (đã có sản phẩm
    /// đóng vào) nhưng KHÔNG tìm thấy bất kỳ <see cref="Domain.Entities.Scan"/> nào gắn
    /// <see cref="Domain.Entities.Scan.PackingBoxId"/> = thùng này — nghĩa là thùng được mở/đóng TRƯỚC thời điểm
    /// triển khai <see cref="Domain.Entities.Scan.PackingBoxId"/> (không backfill dữ liệu cũ). Client PHẢI hiển
    /// thị rõ "Không có dữ liệu chi tiết lượt scan" trong trường hợp này, KHÔNG hiển thị bảng rỗng gây hiểu nhầm là
    /// "0 lượt scan thật". <c>true</c> cho mọi trường hợp còn lại, kể cả thùng thật sự chưa có lượt scan nào
    /// (<see cref="Domain.Entities.PackingBox.ScannedQuantity"/> = 0, vd thùng vừa mở).
    /// </summary>
    public bool HasDetailedScanData { get; set; }

    public IReadOnlyList<PackingProgressReportBoxScanDto> Scans { get; set; } = Array.Empty<PackingProgressReportBoxScanDto>();
}
