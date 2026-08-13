using ProductionMES.Application.DTOs.Scans;

namespace ProductionMES.Application.Abstractions.Realtime;

/// <summary>
/// Abstraction bắn sự kiện real-time khi có lượt scan mới được ghi nhận OK (US-07 AC2/AC3, FR-09). Đặt ở tầng
/// Application để <c>ScanService</c> không phụ thuộc trực tiếp SignalR/tầng Api — implementation thật (dùng
/// <c>IHubContext&lt;ScanHub&gt;</c>) nằm ở <c>ProductionMES.Api</c> (nơi <c>ScanHub</c> được khai báo), đăng
/// ký qua DI ở <c>Program.cs</c>.
/// </summary>
public interface IScanNotifier
{
    /// <summary>
    /// Gọi sau khi lưu thành công 1 lượt scan có kết quả OK — trạm tương lai (Station.Wpf) subscribe theo
    /// <paramref name="workStationId"/> để cập nhật số lượng/chỉ số +/- real-time (FR-09). UI thật hiển thị tại
    /// trạm chưa triển khai ở đợt US-07/US-08 này (xem báo cáo bàn giao) — hạ tầng sự kiện đã sẵn sàng để dùng.
    /// </summary>
    Task NotifyScanRecordedAsync(int workStationId, ScanResultDto scanResult, CancellationToken cancellationToken = default);
}
