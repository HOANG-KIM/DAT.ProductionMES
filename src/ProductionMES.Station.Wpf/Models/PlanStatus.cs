namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror của <c>ProductionMES.Domain.Enums.PlanStatus</c> phía backend (US-05a/FR-05a) — Station.Wpf không
/// reference project backend nào (CLAUDE.md), nên định nghĩa lại đúng tên/giá trị để deserialize JSON dạng chuỗi.
/// </summary>
public enum PlanStatus
{
    Draft = 0,
    Running = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4,
}
