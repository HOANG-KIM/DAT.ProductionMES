namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// US-23 (FR-23, phạm vi thu hẹp 19/08/2026 — CHỈ báo cáo "Theo Lot", chưa làm FR-20/tab "Theo Line") — sinh file
/// Excel (.xlsx) từ đúng dữ liệu <see cref="ILotReportService.GetLotSummaryAsync"/> đang hiển thị trên UI
/// (<c>LotReportTab</c>), cộng thêm chi tiết toàn bộ lượt scan của Lot (Sheet 2) — KHÔNG thêm dữ liệu nào ngoài
/// những gì Service báo cáo hiện có đang trả về.
/// </summary>
public interface ILotReportExportService
{
    /// <summary>
    /// Sinh file .xlsx gồm 2 sheet ("Tổng hợp", "Chi tiết lượt scan") cho đúng <paramref name="lot"/>, lọc theo
    /// khoảng thời gian tùy chọn — CÙNG bộ lọc đang truyền cho <see cref="ILotReportService.GetLotSummaryAsync"/>.
    /// Trả về <c>null</c> khi Lot không tồn tại (AC2 gốc "Không tìm thấy Lot" — Controller quy đổi 404, giống
    /// <c>LotReportsController.GetSummary</c>), KHÔNG throw exception.
    /// </summary>
    Task<byte[]?> ExportAsync(string lot, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
