namespace ProductionMES.Application.DTOs.BreakWindows;

/// <summary>Request thêm 1 khung giờ nghỉ cho Line (AC1).</summary>
public class CreateBreakWindowRequest
{
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Note { get; set; }
}
