using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.PackingBoxes;

/// <summary>DTO trả về cho client, đại diện 1 thùng tại công đoạn "Đóng thùng" (US-25).</summary>
public class PackingBoxDto
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int WorkStationId { get; set; }

    public int BoxNo { get; set; }

    public PackingBoxStatus Status { get; set; }

    public int TargetQuantity { get; set; }

    public int ScannedQuantity { get; set; }

    /// <summary>Snapshot tại thời điểm mở thùng (AC12) — dùng để hiển thị/in lại, KHÔNG phải giá trị hiện tại của PackingModelConfig.</summary>
    public string Model { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public decimal? GrossWeight { get; set; }

    public DateTime OpenedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
