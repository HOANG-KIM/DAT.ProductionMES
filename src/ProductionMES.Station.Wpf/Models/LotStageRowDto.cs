namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>LotStageRowDto</c> phía backend (US-21 AC4, US-21a AC5) — 1 dòng (Line, Công đoạn) đã chạy OK của 1 Lot.</summary>
public class LotStageRowDto
{
    public int LineId { get; set; }

    public string LineName { get; set; } = string.Empty;

    public int StageId { get; set; }

    public string StageName { get; set; } = string.Empty;

    public int OkCount { get; set; }

    public int NgCount { get; set; }

    /// <summary>US-21a AC5 — so <see cref="OkCount"/> với "Tổng số lượng Lot", null khi chưa xác định.</summary>
    public bool? IsSufficientQuantity { get; set; }
}
