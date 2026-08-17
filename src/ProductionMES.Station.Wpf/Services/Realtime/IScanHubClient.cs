using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Realtime;

/// <summary>
/// Kết nối SignalR tới <c>ScanHub</c> (US-07 AC2) — join nhóm theo <c>WorkStationId</c> của trạm rồi lắng nghe
/// sự kiện scan OK mới, để cập nhật số lượng đã scan tại trạm theo thời gian thực (không phụ thuộc riêng vào
/// response của chính request POST vừa gửi).
/// </summary>
public interface IScanHubClient
{
    /// <summary>Bắn khi nhận được sự kiện <c>ScanRecorded</c> — luôn là scan OK của đúng trạm mình (server đã lọc theo group).</summary>
    event Action<ScanResultDto>? ScanRecorded;

    /// <summary>Mở kết nối tới hub và join nhóm của trạm — gọi 1 lần lúc app khởi động. Lỗi kết nối (mạng/server chưa chạy) không throw ra ngoài, tự thử kết nối lại (automatic reconnect).</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync();
}
