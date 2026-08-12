namespace ProductionMES.Application.DTOs.WorkStations;

/// <summary>Request tạo mới 1 trạm làm việc, gắn đúng 1 Line và 1 công đoạn (AC1).</summary>
public class CreateWorkStationRequest
{
    public string Name { get; set; } = string.Empty;

    public int LineId { get; set; }

    public int StageId { get; set; }

    /// <summary>Bật/tắt sử dụng Arduino tại trạm (US-11). Khi true, bắt buộc kèm thông tin cổng COM (AC2).</summary>
    public bool UseArduino { get; set; }

    /// <summary>Bắt buộc khi <see cref="UseArduino"/> = true (AC2), không bắt buộc khi false (AC3).</summary>
    public string? ComPort { get; set; }

    public int? BaudRate { get; set; }

    public string? CommandProtocol { get; set; }
}
