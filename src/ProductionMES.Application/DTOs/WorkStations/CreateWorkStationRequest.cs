namespace ProductionMES.Application.DTOs.WorkStations;

/// <summary>Request tạo mới 1 trạm làm việc, gắn đúng 1 Line và 1 công đoạn (AC1).</summary>
public class CreateWorkStationRequest
{
    public string Name { get; set; } = string.Empty;

    public int LineId { get; set; }

    public int StageId { get; set; }

    /// <summary>"Trạm có máy thông điện" (US-11). Cấu hình COM nay ở appsettings.json cục bộ tại trạm — không còn bắt buộc ComPort/BaudRate/CommandProtocol (AC12).</summary>
    public bool UseArduino { get; set; }

    /// <summary>Không còn dùng để kết nối (US-11 AC12) — giữ để tương thích.</summary>
    public string? ComPort { get; set; }

    public int? BaudRate { get; set; }

    public string? CommandProtocol { get; set; }
}
