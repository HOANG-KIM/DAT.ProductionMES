namespace ProductionMES.Application.DTOs.Lots;

/// <summary>DTO cho entity <see cref="Domain.Entities.Lot"/> (US-21a, viết lại hoàn toàn 19/08/2026).</summary>
public class LotDto
{
    public string Code { get; set; } = string.Empty;

    /// <summary>Tổng số lượng Lot — nhập tay, <c>null</c> = "Chưa xác định" (AC6).</summary>
    public int? TotalQuantity { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? UpdatedByUserName { get; set; }
}
