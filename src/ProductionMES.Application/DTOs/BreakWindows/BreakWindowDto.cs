namespace ProductionMES.Application.DTOs.BreakWindows;

/// <summary>DTO trả về cho client, đại diện 1 khung giờ nghỉ của Line (US-01a).</summary>
public class BreakWindowDto
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Note { get; set; }
}
