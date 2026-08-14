namespace ProductionMES.Application.DTOs.ProductionPlans;

/// <summary>
/// DTO trả về cho client, đại diện 1 kế hoạch sản xuất (US-05, cập nhật FR-05 13/08/2026). KHÔNG có trạng thái
/// vòng đời ở đây — trạng thái (Draft/Running/Paused/Completed/Cancelled) gắn theo cặp (Kế hoạch, Công đoạn),
/// xem <see cref="ProductionPlanStages.ProductionPlanStageDto.PlanStatus"/> (US-05a).
/// </summary>
public class ProductionPlanDto
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    /// <summary>
    /// Sản lượng chuẩn theo giờ = 3600 / Takt time (FR-06/US-06/AC-04) — giá trị dẫn xuất, tính lúc map
    /// Entity → DTO, không lưu cột riêng trong DB.
    /// </summary>
    public decimal StandardQuantityPerHour { get; set; }
}
