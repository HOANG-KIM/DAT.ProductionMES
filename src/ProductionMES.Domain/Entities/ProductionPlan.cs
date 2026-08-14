namespace ProductionMES.Domain.Entities;

/// <summary>
/// Kế hoạch sản xuất — kế hoạch cho 1 Lot sản xuất trên 1 Line cụ thể (FR-05/US-05, cập nhật 13/08/2026 theo
/// FR-05 mới: bổ sung Khách hàng/Model/Lot/Revision, bỏ "Ca làm việc").
/// </summary>
/// <remarks>
/// KHÔNG có trường trạng thái vòng đời (Draft/Running/Paused/Completed/Cancelled) ở entity này. Theo
/// FR-05a/US-05a, trạng thái đó gắn với TỪNG CẶP (Kế hoạch, Công đoạn) — vì các công đoạn khác nhau của cùng 1
/// kế hoạch có thể ở trạng thái khác nhau tại cùng 1 thời điểm (vd công đoạn A chạy đủ số lượng tự đóng
/// <c>Completed</c> trong khi B, C của cùng kế hoạch vẫn <c>Running</c>/<c>Paused</c>). Trạng thái được lưu tại
/// <see cref="ProductionPlanStage.PlanStatus"/> — entity đã đại diện đúng cặp (Kế hoạch, Công đoạn) từ US-03,
/// không cần tạo thêm entity join mới cho việc này. Một kế hoạch mới tạo được coi là "Draft" một cách tự
/// nhiên vì chưa có bản ghi ProductionPlanStage nào (US-03 mới là bước thêm công đoạn vào kế hoạch).
/// </remarks>
public class ProductionPlan
{
    public int Id { get; set; }

    /// <summary>Line áp dụng kế hoạch — mỗi kế hoạch gắn đúng 1 Line, không đổi trong vòng đời (AC1).</summary>
    public int LineId { get; set; }

    /// <summary>Khách hàng (AC1).</summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>Model sản phẩm — thay thế hẳn cặp ProductCode/ProductName cũ (FR-05 cập nhật 13/08/2026).</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Lot sản xuất (AC1).</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Revision — có thể để trống, không bắt buộc (AC2).</summary>
    public string? Revision { get; set; }

    /// <summary>Số lượng kế hoạch, tính theo Lot (AC1).</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// Takt time (giây/sản phẩm) — thời gian tiêu chuẩn để hoàn thành 1 sản phẩm, dùng tính sản lượng chuẩn/giờ
    /// (FR-06/US-06: StandardQuantityPerHour = 3600 / TaktTimeSeconds). Dùng decimal để hỗ trợ takt time có phần thập
    /// phân (thực tế nhà máy không phải lúc nào cũng là số giây nguyên).
    /// </summary>
    public decimal TaktTimeSeconds { get; set; }

    /// <summary>
    /// Thời gian bắt đầu (ngày + giờ) — thay thế cặp EffectiveDate (chỉ có ngày) + Shift (ca làm việc, nhập tự
    /// do) trước đây. FR-05 cập nhật 13/08/2026: Lot + thời gian bắt đầu đã đủ xác định phiên chạy, không cần
    /// "Ca làm việc" riêng.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Tên nhân viên vận hành tại trạm/công đoạn phụ trách lô này — có thể nhiều người, nhập tự do (free text,
    /// vd phân tách bằng dấu phẩy). KHÔNG phải người đăng nhập thao tác màn hình cấu hình kế hoạch (tài khoản đó
    /// đã có audit riêng) — xem FR-05.
    /// </summary>
    public string OperatorNames { get; set; } = string.Empty;
}
