namespace ProductionMES.Application.DTOs.BreakWindows;

/// <summary>Request sửa giờ bắt đầu/kết thúc/ghi chú của 1 khung giờ nghỉ đã tồn tại (AC3).</summary>
public class UpdateBreakWindowRequest
{
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Note { get; set; }
}
