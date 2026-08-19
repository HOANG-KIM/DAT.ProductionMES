namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>LotSummaryDto</c> phía backend (<c>GET api/v1/reports/lots/{lot}</c>, US-21 AC3/AC4/AC5,
/// US-21a AC9 — dùng để "gợi ý tiến độ Lot đã tồn tại" tại màn "Cài đặt kế hoạch"). Chỉ khai báo field UI hiện
/// dùng (System.Text.Json bỏ qua field lạ không khai báo).
/// </summary>
public class LotSummaryDto
{
    public string Lot { get; set; } = string.Empty;

    /// <summary>US-21a AC1/AC5/AC9 — "Tổng số lượng Lot" hiện có (nhập tay), null = "Chưa xác định".</summary>
    public int? LotTotalQuantity { get; set; }

    public List<LotStageRowDto> Rows { get; set; } = new();
}
