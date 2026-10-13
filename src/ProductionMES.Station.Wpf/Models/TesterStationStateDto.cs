namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>TesterStationStateDto</c> phía backend (US-11) — <c>GET api/v1/tester/station-state</c>.</summary>
public class TesterStationStateDto
{
    public int WorkStationId { get; set; }

    /// <summary>"Trạm có máy thông điện" (Admin đặt).</summary>
    public bool UseArduino { get; set; }

    /// <summary>Công tắc runtime "Đang sử dụng máy thông điện" — mặc định BẬT.</summary>
    public bool TesterEnabled { get; set; }
}
