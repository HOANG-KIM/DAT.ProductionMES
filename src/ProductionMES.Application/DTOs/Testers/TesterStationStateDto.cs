namespace ProductionMES.Application.DTOs.Testers;

/// <summary>
/// Trạng thái máy thông điện của một trạm (US-11) — Station.Wpf đọc khi khởi động (xác thực bằng StationApiKey) để
/// quyết định có chạy luồng máy thông điện (US-12) / Bypass / luồng thường hay không.
/// </summary>
public class TesterStationStateDto
{
    public int WorkStationId { get; set; }

    /// <summary>"Trạm có máy thông điện" (<c>WorkStation.UseArduino</c>, Admin đặt).</summary>
    public bool UseArduino { get; set; }

    /// <summary>Công tắc runtime "Đang sử dụng máy thông điện" (<c>WorkStation.TesterEnabled</c>) — mặc định BẬT; chỉ có ý nghĩa khi <see cref="UseArduino"/> = true.</summary>
    public bool TesterEnabled { get; set; }
}
