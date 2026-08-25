using ProductionMES.Application.DTOs.Reports;

namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// Màn hình theo dõi tiến độ đóng thùng ở mức quản lý (US-26/FR-26) — mô hình tra cứu theo Lot (viết lại
/// 25/08/2026, xem README US-26 "Lý do thay đổi"): gõ (một phần) mã Lot để nhận gợi ý các Lot đang <c>Running</c>
/// tại công đoạn "Đóng thùng" (<c>Stage.IsPackingStage = true</c>) trên TOÀN nhà máy (AC1 — <see cref="SearchAsync"/>,
/// KHÔNG lọc theo Line), rồi chọn 1 Lot để xem (các) dòng kết quả tương ứng, đối chiếu với "Tổng số lượng Lot"
/// (US-21a) để tính % hoàn thành (AC2/AC3/AC4 — <see cref="GetReportAsync"/>).
/// </summary>
public interface IPackingProgressReportService
{
    /// <summary>
    /// AC1 (viết lại LẦN 2 — 25/08/2026): gợi ý (autocomplete) các Lot đang <c>Running</c> tại công đoạn "Đóng
    /// thùng" khớp gần đúng <paramref name="search"/>, gộp DUY NHẤT theo Lot (dedupe, KHÔNG lặp lại theo Line) —
    /// truy vấn NHẸ, KHÔNG tính SUM/gộp <c>PackingBox</c> (khác hẳn <see cref="GetReportAsync"/>). Trả mảng rỗng khi
    /// <paramref name="search"/> trống hoặc không khớp Lot nào.
    /// </summary>
    Task<IReadOnlyList<PackingProgressSearchItemDto>> SearchAsync(string? search, CancellationToken cancellationToken = default);

    /// <summary>AC2/AC3/AC4 — (các) dòng kết quả chi tiết ứng với bộ lọc (thường chỉ truyền <c>Lot</c> sau khi đã chọn ở AC1).</summary>
    Task<PackingProgressReportDto> GetReportAsync(PackingProgressReportQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// AC6: danh sách TẤT CẢ thùng (Completed lẫn InProgress) của 1 dòng báo cáo (Line + Lot) — gộp theo TẤT CẢ
    /// <c>ProductionPlan</c> cùng Lot tại cùng (Line, Công đoạn Đóng thùng), không giới hạn <c>PlanStatus</c>, cùng
    /// cách gộp đã dùng cho <see cref="GetReportAsync"/>. Sắp xếp theo <c>BoxNo</c> tăng dần.
    /// </summary>
    Task<IReadOnlyList<PackingProgressReportBoxDto>> GetBoxesAsync(int lineId, string lot, CancellationToken cancellationToken = default);

    /// <summary>
    /// AC7/AC8: danh sách lượt scan OK đã cộng vào 1 thùng cụ thể, sắp xếp theo thời điểm scan tăng dần. Ném
    /// <see cref="Domain.Exceptions.EntityNotFoundException"/> nếu không tìm thấy thùng.
    /// </summary>
    Task<PackingProgressReportBoxScansDto> GetBoxScansAsync(int packingBoxId, CancellationToken cancellationToken = default);
}
