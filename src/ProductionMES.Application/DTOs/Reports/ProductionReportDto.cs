namespace ProductionMES.Application.DTOs.Reports;

/// <summary>Báo cáo tổng hợp ACTUAL/NG/PLAN/BALANCE theo Line/Lot/công đoạn (US-21/FR-21, mở rộng 18/08/2026).</summary>
public class ProductionReportDto
{
    /// <summary>Echo lại filter đã áp dụng (US-21 AC1/AC2) — null/null nghĩa là đang xem theo thời gian thực (AC1).</summary>
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    /// <summary>Thời điểm tạo báo cáo (UTC) — phục vụ debug/hiển thị "cập nhật lúc".</summary>
    public DateTime GeneratedAtUtc { get; set; }

    public IReadOnlyList<ProductionReportRowDto> Rows { get; set; } = Array.Empty<ProductionReportRowDto>();
}
