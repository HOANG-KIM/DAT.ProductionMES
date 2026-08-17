namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>AndonBoardHourRowDto</c> phía backend (US-09 AC6) — 1 dòng trong bảng theo mốc giờ.</summary>
public class AndonBoardHourRowDto
{
    public DateTime TimeMarkLocal { get; set; }

    public int PlanCumulative { get; set; }

    public int ActualCumulative { get; set; }

    public int Balance { get; set; }

    /// <summary>True khi đây là dòng "hiện tại" — UI highlight khác các dòng mốc giờ tròn đã qua.</summary>
    public bool IsCurrent { get; set; }
}
