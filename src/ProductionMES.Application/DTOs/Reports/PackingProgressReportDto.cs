namespace ProductionMES.Application.DTOs.Reports;

/// <summary>Kết quả màn hình theo dõi tiến độ đóng thùng (US-26/FR-26) — bọc thêm <see cref="GeneratedAtUtc"/> cùng quy ước với <see cref="ProductionReportDto"/> (US-21) để client hiển thị "cập nhật lúc" (AC5).</summary>
public class PackingProgressReportDto
{
    /// <summary>Thời điểm tạo báo cáo (UTC) — phục vụ hiển thị "cập nhật lúc" khi polling (AC5), cùng quy ước <c>ProductionReportDto.GeneratedAtUtc</c>.</summary>
    public DateTime GeneratedAtUtc { get; set; }

    public IReadOnlyList<PackingProgressReportRowDto> Rows { get; set; } = Array.Empty<PackingProgressReportRowDto>();
}
