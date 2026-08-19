using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Reports;

/// <summary>
/// Gọi <c>api/v1/reports/lots</c> (US-21, permission <c>Report.View</c>) — dùng RIÊNG cho US-21a AC9 (US-05 AC9):
/// gợi ý "Tổng SL Lot" hiện có + breakdown đã chạy OK theo (Line, Công đoạn) khi Tổ trưởng gõ/chọn 1 Lot đã tồn
/// tại tại màn "Cài đặt kế hoạch" — tái dùng đúng endpoint đã có ở báo cáo Lot-centric, KHÔNG tạo API mới.
/// </summary>
public interface ILotReportApiClient
{
    /// <summary>Trả về <c>null</c> khi Lot chưa từng có kế hoạch sản xuất nào (server trả 404 — AC2 US-21, "Không tìm thấy Lot").</summary>
    Task<LotSummaryDto?> GetSummaryAsync(string lot, CancellationToken cancellationToken = default);
}
