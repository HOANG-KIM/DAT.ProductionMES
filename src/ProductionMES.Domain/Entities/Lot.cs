namespace ProductionMES.Domain.Entities;

/// <summary>
/// Lot sản xuất — entity NHẸ, độc lập với <see cref="ProductionPlan"/> (US-21a/FR-21a, viết lại hoàn toàn
/// 19/08/2026). Chỉ tồn tại để lưu 1 giá trị NHẬP TAY duy nhất ("Tổng số lượng Lot" — số lượng thực của cả lô
/// hàng, biết trước theo đơn hàng), KHÔNG phải giá trị tính toán (SUM) từ <see cref="ProductionPlan.PlannedQuantity"/>.
/// </summary>
/// <remarks>
/// <see cref="Code"/> là khóa nghiệp vụ, khớp giá trị <see cref="ProductionPlan.Lot"/> — KHÔNG có FK/unique
/// constraint ở tầng DB (CLAUDE.md, mục Data access: hệ thống không dùng khoá ngoại). Tính duy nhất của
/// <see cref="Code"/> do tầng Service đảm bảo khi upsert (tìm-rồi-update-hoặc-tạo-mới — xem
/// <c>Services.Lots.LotService.UpsertTotalQuantityAsync</c>), KHÔNG dựa vào constraint của MySQL.
///
/// <see cref="ProductionPlan.Lot"/> (string) GIỮ NGUYÊN không đổi — entity này KHÔNG đụng vào luồng snapshot
/// <c>Scan.Lot</c>/autocomplete/GroupBy đã ổn định ở nhiều Service khác (<c>LotReportService</c>,
/// <c>ProductionReportService</c>...).
///
/// Row <see cref="Lot"/> được tạo LAZY — chỉ sinh khi thực sự có người nhập <see cref="TotalQuantity"/> lần đầu
/// (KHÔNG tự tạo rỗng cho mọi giá trị Lot xuất hiện trên <see cref="ProductionPlan"/>).
/// </remarks>
public class Lot
{
    public int Id { get; set; }

    /// <summary>Khóa nghiệp vụ — khớp giá trị <see cref="ProductionPlan.Lot"/> (so khớp tuyệt đối, không phân biệt hoa/thường theo collation MySQL mặc định).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số lượng Lot — NHẬP TAY (US-21a AC1), KHÔNG phải SUM. <c>null</c> nghĩa là "Chưa xác định" (AC6) —
    /// chưa từng có ai nhập giá trị này cho Lot này.
    /// </summary>
    public int? TotalQuantity { get; set; }

    /// <summary>
    /// Thời điểm cập nhật gần nhất — lưu ĐÚNG giờ local hệ thống, KHÔNG quy đổi UTC (đổi ý 19/08/2026, xem
    /// API-Conventions.md mục 10) — mỗi lần upsert đều ghi lại, kể cả khi giá trị không đổi.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Tên đăng nhập người cập nhật gần nhất (snapshot, không tra cứu động) — có thể null nếu không xác định được người thực hiện (vd gọi Service trực tiếp không qua Controller).</summary>
    public string? UpdatedByUserName { get; set; }
}
