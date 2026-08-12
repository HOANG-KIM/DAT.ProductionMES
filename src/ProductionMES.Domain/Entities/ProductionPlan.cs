namespace ProductionMES.Domain.Entities;

/// <summary>
/// Kế hoạch sản xuất — kế hoạch cho 1 lô/ca sản xuất trên 1 Line cụ thể (FR-05/US-05).
/// </summary>
/// <remarks>
/// AC1 (US-05): kế hoạch được tạo trước ở trạng thái chưa active (<see cref="IsActive"/> = false),
/// và được kích hoạt riêng qua 1 thao tác khác (xem ứng dụng nghiệp vụ ở ProductionPlanService.ActivateAsync) —
/// vì AC1 mô tả "kế hoạch được tạo, <b>có thể được kích hoạt sau đó</b>", ngụ ý 2 bước tách biệt, không tự
/// động active ngay khi tạo. AC2: 1 Line chỉ có 1 kế hoạch active tại 1 thời điểm — validate khi kích hoạt.
/// </remarks>
public class ProductionPlan
{
    public int Id { get; set; }

    /// <summary>Line áp dụng kế hoạch — mỗi kế hoạch gắn đúng 1 Line (AC1).</summary>
    public int LineId { get; set; }

    /// <summary>Mã sản phẩm (AC1).</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Tên sản phẩm (AC1).</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Số lượng kế hoạch (AC1).</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// Takt time (giây/sản phẩm) — thời gian tiêu chuẩn để hoàn thành 1 sản phẩm, dùng tính sản lượng chuẩn/giờ
    /// (FR-06/US-06: StandardQuantityPerHour = 3600 / TaktTimeSeconds). Dùng decimal để hỗ trợ takt time có phần thập
    /// phân (thực tế nhà máy không phải lúc nào cũng là số giây nguyên).
    /// </summary>
    public decimal TaktTimeSeconds { get; set; }

    /// <summary>Ca làm việc áp dụng kế hoạch (vd "Ca 1", "Ca 2", "Hành chính") — nhập tự do (AC1).</summary>
    public string Shift { get; set; } = string.Empty;

    /// <summary>Ngày áp dụng kế hoạch (AC1).</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Trạng thái active của kế hoạch. Mặc định <c>false</c> khi tạo mới (AC1 — kích hoạt là thao tác riêng).
    /// AC2: tại 1 thời điểm, 1 Line chỉ được có tối đa 1 kế hoạch <c>IsActive = true</c>.
    /// </summary>
    public bool IsActive { get; set; }
}
