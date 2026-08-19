namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>ProductionPlanDto</c> phía backend (US-05).</summary>
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

    public decimal StandardQuantityPerHour { get; set; }

    /// <summary>
    /// US-05 AC7/AC9 (=US-21a) — "Tổng số lượng Lot" hiện có của <see cref="Lot"/> (nhập tay), <c>null</c> = "Chưa
    /// xác định". Hiển thị lại tự động khi mở kế hoạch để sửa (AC2 US-21a).
    /// </summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>Hiển thị Takt time dạng "m:ss" (US-05 AC1e) — thay cho hiển thị số giây thô trên DataGrid.</summary>
    public string TaktTimeDisplay => TaktTimeFormat.ToDisplay(TaktTimeSeconds);

    /// <summary>Hiển thị Thời gian bắt đầu dạng "dd/MM/yyyy HH:mm" (US-05 AC1d) — thay cho ToString mặc định .NET.</summary>
    public string StartTimeDisplay => StartTime.ToString("dd/MM/yyyy HH:mm");
}
