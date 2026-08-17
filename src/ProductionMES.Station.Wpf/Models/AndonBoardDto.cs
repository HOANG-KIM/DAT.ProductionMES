namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>AndonBoardDto</c> phía backend (US-09, response <c>GET api/v1/andon-board</c>) — chỉ khai
/// báo field UI hiện dùng, đủ để deserialize (System.Text.Json bỏ qua field lạ không khai báo).
/// </summary>
public class AndonBoardDto
{
    public int WorkStationId { get; set; }

    public int LineId { get; set; }

    public int StageId { get; set; }

    /// <summary>False khi (Line, Công đoạn) của trạm chưa có kế hoạch nào đang Running.</summary>
    public bool HasActivePlan { get; set; }

    public int? ProductionPlanId { get; set; }

    public int ActualCumulative { get; set; }

    public int PlanCumulative { get; set; }

    public int Balance { get; set; }

    public int NgCount { get; set; }

    public decimal NgPercent { get; set; }

    public DateTime GeneratedAtLocal { get; set; }

    public List<AndonBoardHourRowDto> Rows { get; set; } = new();
}
