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

    /// <summary>Model sản phẩm — rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Lot sản xuất — rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Số lượng kế hoạch (PROD.PLAN) — 0 khi <see cref="HasActivePlan"/> = false.</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>Takt time (giây/sản phẩm) — format phút:giây làm ở client bằng <c>TaktTimeFormat.ToDisplay</c> (US-05).</summary>
    public decimal TaktTimeSeconds { get; set; }

    /// <summary>Thời gian bắt đầu kế hoạch (STARTING TIME) — null khi <see cref="HasActivePlan"/> = false.</summary>
    public DateTime? PlanStartTime { get; set; }

    /// <summary>Tên nhân viên vận hành (ô PER, free text) — rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    public string OperatorNames { get; set; } = string.Empty;

    public int ActualCumulative { get; set; }

    public int PlanCumulative { get; set; }

    public int Balance { get; set; }

    public int NgCount { get; set; }

    public decimal NgPercent { get; set; }

    public DateTime GeneratedAtLocal { get; set; }

    public List<AndonBoardHourRowDto> Rows { get; set; } = new();
}
