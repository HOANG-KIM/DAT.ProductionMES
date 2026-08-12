namespace ProductionMES.Application.DTOs.WorkStations;

/// <summary>Request cập nhật 1 trạm làm việc đã tồn tại. Đổi trạng thái hoạt động dùng endpoint riêng.</summary>
public class UpdateWorkStationRequest
{
    public string Name { get; set; } = string.Empty;

    public int LineId { get; set; }

    public int StageId { get; set; }

    public bool UseArduino { get; set; }

    public string? ComPort { get; set; }

    public int? BaudRate { get; set; }

    public string? CommandProtocol { get; set; }
}
