namespace ProductionMES.Application.DTOs.Reports;

/// <summary>1 kết quả gợi ý Lot (US-21 AC1/AC2) — chỉ có mã Lot, không có entity Lot riêng (CLAUDE.md).</summary>
public class LotSearchItemDto
{
    public string Lot { get; set; } = string.Empty;
}
