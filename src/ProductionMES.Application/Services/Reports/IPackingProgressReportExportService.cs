namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// US-26 (AC9-AC13, bổ sung 25/08/2026) — sinh file Excel (.xlsx, 3 sheet: "Tổng quan"/"Danh sách thùng"/"Lượt
/// scan") cho ĐÚNG 1 dòng báo cáo (Line + Lot) ở màn hình theo dõi tiến độ đóng thùng. Tái dùng NGUYÊN VẸN
/// <see cref="IPackingProgressReportService.GetReportAsync"/> (Sheet "Tổng quan"),
/// <see cref="IPackingProgressReportService.GetBoxesAsync"/> (Sheet "Danh sách thùng", AC6) và
/// <see cref="IPackingProgressReportService.GetBoxScansAsync"/> (Sheet "Lượt scan", AC7/AC8) — gọi lại NGAY tại
/// thời điểm xuất để đảm bảo dữ liệu mới nhất (AC9), KHÔNG dùng dữ liệu cache đang hiển thị trên UI.
/// </summary>
public interface IPackingProgressReportExportService
{
    /// <summary>
    /// Sinh file .xlsx cho dòng báo cáo khớp đúng <paramref name="lineId"/> + <paramref name="lot"/>. Trả về
    /// <c>null</c> khi KHÔNG còn dòng nào khớp tại thời điểm gọi (vd kế hoạch không còn <c>Running</c> nữa) —
    /// Controller quy đổi 404, cùng quy ước <see cref="ILotReportExportService.ExportAsync"/>.
    /// </summary>
    Task<byte[]?> ExportAsync(int lineId, string lot, CancellationToken cancellationToken = default);
}
