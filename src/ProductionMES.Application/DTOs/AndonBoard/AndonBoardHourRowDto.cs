namespace ProductionMES.Application.DTOs.AndonBoard;

/// <summary>1 dòng trong bảng theo dõi sản lượng theo mốc giờ tại màn hình trạm (US-09 AC6).</summary>
public class AndonBoardHourRowDto
{
    /// <summary>
    /// Mốc giờ hiển thị (giờ tường tại nhà máy — cùng quy ước với <c>ProductionPlan.StartTime</c>, xem remarks
    /// tại <c>AndonBoardCalculator</c>). Dòng "hiện tại" (<see cref="IsCurrent"/> = true) không tròn giờ, các
    /// dòng còn lại là mốc StartTime + N giờ tròn.
    /// </summary>
    public DateTime TimeMarkLocal { get; set; }

    public int PlanCumulative { get; set; }

    public int ActualCumulative { get; set; }

    public int Balance { get; set; }

    /// <summary>
    /// True khi đây là dòng "hiện tại" (mockup Andon board — TIME không tròn giờ, cập nhật liên tục) — UI
    /// highlight khác các dòng mốc giờ tròn đã qua.
    /// </summary>
    public bool IsCurrent { get; set; }
}
