namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>WorkStationDto</c> phía backend (US-04, trạm làm việc).</summary>
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
