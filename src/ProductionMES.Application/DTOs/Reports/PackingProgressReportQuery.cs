namespace ProductionMES.Application.DTOs.Reports;

/// <summary>Bộ lọc màn hình theo dõi tiến độ đóng thùng (US-26/FR-26, AC4) — tất cả tùy chọn, kết hợp AND với nhau và với nguồn dữ liệu gốc (mọi kế hoạch đang Running tại công đoạn Đóng thùng).</summary>
public class PackingProgressReportQuery
{
    /// <summary>Lọc theo đúng 1 Line — tùy chọn.</summary>
    public int? LineId { get; set; }

    /// <summary>Lọc theo đúng 1 Lot (so khớp tuyệt đối, khớp <c>ProductionPlan.Lot</c> của kế hoạch đang Running) — tùy chọn.</summary>
    public string? Lot { get; set; }

    /// <summary>Lọc theo đúng 1 Model (so khớp tuyệt đối, khớp <c>ProductionPlan.Model</c> của kế hoạch đang Running) — tùy chọn.</summary>
    public string? Model { get; set; }
}
