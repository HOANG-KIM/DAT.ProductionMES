namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// 1 kết quả gợi ý (autocomplete) Lot đang chạy công đoạn "Đóng thùng" (US-26/FR-26, AC1 — viết lại LẦN 2
/// 25/08/2026). Gộp DUY NHẤT theo Lot (dedupe — KHÔNG lặp lại theo Line dù Lot đó đang chạy đồng thời nhiều Line),
/// giống HỆT <see cref="LotSearchItemDto"/> (US-21) — việc phân biệt theo Line dời hẳn xuống bảng kết quả +
/// dropdown lọc Line (AC2), không còn hiển thị ở bước gợi ý này nữa.
/// </summary>
public class PackingProgressSearchItemDto
{
    public string Lot { get; set; } = string.Empty;
}
