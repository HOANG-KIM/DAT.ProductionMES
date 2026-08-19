namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>UpdateProductionPlanRequest</c> phía backend (US-05 AC4/AC5/AC6).</summary>
public class UpdateProductionPlanRequest
{
    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    /// <summary>US-05 AC7 (=US-21a AC1) — bắt buộc khi Lot (sau khi sửa) hoàn toàn mới, server tự kiểm tra (409 nếu thiếu).</summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>AC5: xác nhận sửa Số lượng/Takt time khi kế hoạch đã có công đoạn Running/Paused. Dùng CHUNG cho AC8 (US-21a AC3 — giảm "Tổng số lượng Lot" dưới thực tế).</summary>
    public bool Confirm { get; set; }
}
