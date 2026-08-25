using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// 1 dòng trong màn hình theo dõi tiến độ đóng thùng (US-26/FR-26, AC2) — đại diện đúng 1 cặp (Kế hoạch sản
/// xuất, Công đoạn "Đóng thùng"). Bản ghi đại diện được chọn theo thứ tự ưu tiên PlanStatus
/// (AC14, viết lại LẦN 3 — 25/08/2026): <c>Running</c> &gt; <c>Paused</c> &gt; <c>Completed</c> &gt; <c>Cancelled</c>
/// (loại <c>Draft</c>) — xem <see cref="PackingProgressReportRowDto.PlanStatus"/>. Số thùng/số lượng đã đóng OK GỘP (SUM) thêm cả những
/// <see cref="Domain.Entities.PackingBox"/> thuộc các <see cref="Domain.Entities.ProductionPlan"/> KHÁC cùng Lot +
/// cùng (Line, Công đoạn) này (không giới hạn <c>PlanStatus</c>) — xử lý đúng trường hợp kế hoạch cũ bị
/// <c>Cancelled</c> rồi tạo lại cho cùng 1 Lot (cùng Quyết định 18/08/2026 đã áp dụng ở
/// <c>ProductionReportService</c>/US-21, xem remarks ở đó).
/// </summary>
public class PackingProgressReportRowDto
{
    /// <summary>Id kế hoạch sản xuất của bản ghi đại diện (AC14) tại công đoạn Đóng thùng — dùng để client drill-down nếu cần.</summary>
    public int ProductionPlanId { get; set; }

    public int LineId { get; set; }

    public string LineName { get; set; } = string.Empty;

    public int StageId { get; set; }

    public string StageName { get; set; } = string.Empty;

    /// <summary>Model của kế hoạch đại diện (AC2/AC14).</summary>
    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái kế hoạch của bản ghi đại diện (AC14, viết lại LẦN 3 — 25/08/2026) — <c>Running</c>/<c>Paused</c>/
    /// <c>Completed</c>/<c>Cancelled</c> (không bao giờ <c>Draft</c>, đã loại khi chọn dòng). Serialize dạng chuỗi
    /// (API-Conventions.md mục 10).
    /// </summary>
    public PlanStatus PlanStatus { get; set; }

    /// <summary>Số thùng đã đóng xong (COUNT <c>PackingBox.Status = Completed</c>, đã gộp theo Lot — xem tóm tắt lớp) (AC1).</summary>
    public int CompletedBoxCount { get; set; }

    /// <summary>Tổng số lượng sản phẩm OK đã đóng thùng (SUM <c>PackingBox.ScannedQuantity</c> của các thùng Completed, đã gộp theo Lot) — KHÔNG cộng thùng đang <c>InProgress</c> dở (AC1).</summary>
    public int PackedOkQuantity { get; set; }

    /// <summary>
    /// "Tổng số lượng Lot" nhập tay (US-21a, entity <see cref="Domain.Entities.Lot"/>). <c>null</c> = "Chưa xác
    /// định" (AC3) — Lot chưa từng có ai nhập giá trị này.
    /// </summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>
    /// % hoàn thành = <see cref="PackedOkQuantity"/> / <see cref="LotTotalQuantity"/> × 100, làm tròn 2 chữ số
    /// thập phân (AC2). <c>null</c> khi <see cref="LotTotalQuantity"/> = <c>null</c> (AC3 "Chưa xác định") — client
    /// PHẢI hiển thị rõ "Chưa xác định", KHÔNG được suy diễn 0%.
    /// </summary>
    public decimal? CompletionPercentage { get; set; }

    /// <summary>
    /// Nhãn "Đủ"/"Chưa đủ" khi đạt/vượt 100% — cùng quy ước hiển thị đã chốt ở US-21/US-21a
    /// (<c>LotStageRowDto.IsSufficientQuantity</c>), KHÔNG có khái niệm "hoàn thành" riêng nào khác (AC2). <c>null</c>
    /// khi <see cref="LotTotalQuantity"/> = <c>null</c>.
    /// </summary>
    public bool? IsSufficientQuantity { get; set; }
}
