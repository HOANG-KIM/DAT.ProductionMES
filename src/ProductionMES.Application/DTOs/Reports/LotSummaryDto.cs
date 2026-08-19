namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// Chi tiết vòng đời sản xuất của 1 Lot (US-21 AC3/AC4/AC5, vòng 3 — Lot-centric; US-21a AC5/AC6, viết lại hoàn
/// toàn 19/08/2026). Trả về <c>null</c> ở tầng Service khi Lot không tồn tại (AC2 — "Không tìm thấy Lot", KHÔNG
/// phải lỗi hệ thống, Controller trả 404).
/// </summary>
public class LotSummaryDto
{
    public string Lot { get; set; } = string.Empty;

    /// <summary>
    /// AC3: TẤT CẢ giá trị Model khác nhau tìm được trên các <c>ProductionPlan</c> cùng Lot — 1 phần tử nếu mọi
    /// kế hoạch đồng nhất, &gt;1 phần tử nếu KHÔNG đồng nhất (client tự hiển thị cảnh báo khi Count &gt; 1, Service
    /// không tự chọn 1 giá trị đại diện).
    /// </summary>
    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();

    /// <summary>AC3 — tương tự <see cref="Models"/>, cho Khách hàng (Customer).</summary>
    public IReadOnlyList<string> Customers { get; set; } = Array.Empty<string>();

    /// <summary>
    /// AC3 — tương tự <see cref="Models"/>, cho Revision. Chuỗi rỗng (<c>""</c>) đại diện cho giá trị Revision =
    /// <c>null</c> (không nhập) trên <c>ProductionPlan</c> nào đó — KHÔNG dùng <c>null</c> trong danh sách để tránh
    /// vấn đề serialize JSON lẫn với "chưa tải xong".
    /// </summary>
    public IReadOnlyList<string> Revisions { get; set; } = Array.Empty<string>();

    /// <summary>AC5 — echo lại filter khoảng thời gian đã áp dụng. Null/null = tính toàn bộ lịch sử (không giới hạn).</summary>
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    /// <summary>AC4 — danh sách (Line, Công đoạn) Lot này đã từng sản xuất, kèm OK/NG + Đủ/Chưa đủ (US-21a AC5).</summary>
    public IReadOnlyList<LotStageRowDto> Rows { get; set; } = Array.Empty<LotStageRowDto>();

    /// <summary>
    /// US-21a AC1/AC5 (viết lại hoàn toàn 19/08/2026) — "Tổng số lượng Lot" NHẬP TAY (entity <see cref="Domain.Entities.Lot"/>,
    /// KHÔNG phải SUM(PlannedQuantity)). <c>null</c> = "Chưa xác định" (AC6 — Lot chưa từng có ai nhập giá trị này).
    /// </summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>
    /// FR-21b (bổ sung 19/08/2026) — "Thời gian bắt đầu" = <c>MIN(Scan.ScannedAtUtc)</c> trên TOÀN BỘ lượt scan có
    /// <c>Scan.Lot = Lot</c> (mọi <c>Result</c>, kể cả bị từ chối; không phân biệt Line/Công đoạn). KHÔNG bị ảnh
    /// hưởng bởi <see cref="FromUtc"/>/<see cref="ToUtc"/> — luôn là mốc gốc tuyệt đối trên toàn bộ lịch sử Lot.
    /// KHÔNG liên quan <c>ProductionPlan.StartTime</c> (nhập tay, dùng cho PLAN/BALANCE Andon board). <c>null</c> =
    /// "Chưa xác định" (Lot chưa từng có lượt scan nào).
    /// </summary>
    public DateTime? FirstScannedAtUtc { get; set; }
}
