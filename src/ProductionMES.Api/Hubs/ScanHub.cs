using Microsoft.AspNetCore.SignalR;

namespace ProductionMES.Api.Hubs;

/// <summary>
/// Hub SignalR phục vụ cập nhật real-time số lượng scan/chỉ số +/- tới các trạm/màn hình liên quan (FR-09).
/// Chưa khai báo method/nhóm cụ thể ở bước scaffold này — sẽ bổ sung khi triển khai ScanService.
/// </summary>
public class ScanHub : Hub
{
}
