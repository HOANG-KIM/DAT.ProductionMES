namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>CreateProductionPlanRequest</c> phía backend (US-05 AC1).</summary>
public class CreateProductionPlanRequest
{
    public int LineId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    /// <summary>US-05 AC7 (=US-21a AC1) — bắt buộc khi Lot hoàn toàn mới, server tự kiểm tra (409 nếu thiếu).</summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>US-05 AC8 (=US-21a AC3) — xác nhận khi Lot đã tồn tại có "Tổng số lượng Lot" giảm dưới thực tế đã chạy.</summary>
    public bool Confirm { get; set; }
}
