namespace ProductionMES.Station.Tester;

/// <summary>
/// Cấu hình kết nối máy thông điện, đọc từ section <c>Tester</c> trong <c>Station.Wpf/appsettings.json</c> (cục bộ tại
/// từng trạm, Options pattern — US-14 "Cấu hình cục bộ"). Không có màn hình Cài đặt trong app: sửa file khi triển khai.
/// </summary>
public class TesterOptions
{
    public const string SectionName = "Tester";

    /// <summary>Tên cổng COM (vd <c>COM5</c>) — cổng USB ghi "UART" trên DevKitC-1. Rỗng = chưa cấu hình, App báo mất kết nối.</summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>Baud rate, mặc định 115200 (8N1).</summary>
    public int BaudRate { get; set; } = 115200;

    /// <summary>Chu kỳ gửi <c>#PING</c> (ms).</summary>
    public int PingIntervalMs { get; set; } = 1000;

    /// <summary>Quá thời gian này không nhận <c>#PONG</c> thì coi là mất kết nối (ms).</summary>
    public int LinkTimeoutMs { get; set; } = 5000;

    /// <summary>Chờ phản hồi cho mỗi lệnh BIND/SAVED/DISCARD/RESULT?/HELLO? (ms).</summary>
    public int CommandTimeoutMs { get; set; } = 2000;

    /// <summary>Chu kỳ thử mở lại cổng khi mất kết nối (ms).</summary>
    public int ReconnectIntervalMs { get; set; } = 2000;
}
