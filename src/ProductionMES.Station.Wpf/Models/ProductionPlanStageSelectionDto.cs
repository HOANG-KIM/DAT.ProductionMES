namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ProductionPlanStageSelectionDto</c> phía backend (US-05b, GET /production-plan-stages).</summary>
public class ProductionPlanStageSelectionDto
{
    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int LineId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    public decimal StandardQuantityPerHour { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    public PlanStatus PlanStatus { get; set; }

    public int RunCount { get; set; }

    public int RemainingCount { get; set; }

    /// <summary>
    /// US-21a AC7 (viết lại hoàn toàn 19/08/2026) — "Tổng số lượng Lot" NHẬP TAY, KHÔNG phải SUM(PlannedQuantity).
    /// <c>null</c> = "Chưa xác định" (chưa từng có ai nhập giá trị này cho Lot này).
    /// </summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>Hiển thị Takt time dạng "m:ss" (US-05 AC1e, US-05b AC4) — thay cho hiển thị số giây thô.</summary>
    public string TaktTimeDisplay => TaktTimeFormat.ToDisplay(TaktTimeSeconds);

    /// <summary>Hiển thị Thời gian bắt đầu dạng "dd/MM/yyyy HH:mm" (US-05 AC1d, US-05b AC4) — thay cho ToString mặc định .NET.</summary>
    public string StartTimeDisplay => StartTime.ToString("dd/MM/yyyy HH:mm");

    /// <summary>US-21a AC7 — giá trị hiển thị thô (hoặc "Chưa xác định") — dùng cho cột DataGrid đã có sẵn tiêu đề "SL Lot".</summary>
    public string LotTotalQuantityValueDisplay => LotTotalQuantity.HasValue ? LotTotalQuantity.Value.ToString() : "Chưa xác định";

    /// <summary>US-21a AC7 — nhãn "SL Lot: &lt;giá trị hoặc 'Chưa xác định'&gt;" hiển thị cạnh Số lượng kế hoạch riêng của dòng đó (khu vực chi tiết, không có tiêu đề cột sẵn).</summary>
    public string LotTotalQuantityDisplay => $"SL Lot: {LotTotalQuantityValueDisplay}";
}
