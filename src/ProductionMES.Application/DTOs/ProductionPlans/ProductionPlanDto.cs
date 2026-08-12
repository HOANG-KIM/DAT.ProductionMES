namespace ProductionMES.Application.DTOs.ProductionPlans;

/// <summary>DTO trả về cho client, đại diện 1 kế hoạch sản xuất (US-05).</summary>
public class ProductionPlanDto
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public string Shift { get; set; } = string.Empty;

    public DateTime EffectiveDate { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Sản lượng chuẩn theo giờ = 3600 / Takt time (FR-06/US-06/AC-04) — giá trị dẫn xuất, tính lúc map
    /// Entity → DTO, không lưu cột riêng trong DB.
    /// </summary>
    public decimal StandardQuantityPerHour { get; set; }
}
