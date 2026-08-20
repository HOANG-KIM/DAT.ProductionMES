namespace ProductionMES.Application.DTOs.Reports;

/// <summary>1 dòng lịch sử thay đổi "Tổng số lượng Lot" — khớp entity <c>LotHistory</c>, mới nhất trước.</summary>
public class LotHistoryItemDto
{
    /// <summary><c>null</c> nếu đây là lần đầu tiên Lot này được đặt giá trị.</summary>
    public int? OldTotalQuantity { get; set; }

    public int NewTotalQuantity { get; set; }

    /// <summary>Giờ tường tại nhà máy (giờ Việt Nam), KHÔNG quy đổi UTC — xem API-Conventions.md mục 10.</summary>
    public DateTime ChangedAtUtc { get; set; }

    public string? ChangedByUserName { get; set; }
}
