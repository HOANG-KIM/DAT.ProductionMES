namespace ProductionMES.Application.DTOs.WorkStations;

/// <summary>DTO trả về cho client, đại diện 1 trạm làm việc (US-04).</summary>
public class WorkStationDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int LineId { get; set; }

    public int StageId { get; set; }

    public bool UseArduino { get; set; }

    public string? ComPort { get; set; }

    public int? BaudRate { get; set; }

    public string? CommandProtocol { get; set; }

    public bool IsActive { get; set; }
}
