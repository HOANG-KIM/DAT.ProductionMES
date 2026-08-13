using Microsoft.AspNetCore.SignalR;

namespace ProductionMES.Api.Hubs;

/// <summary>
/// Hub SignalR phục vụ cập nhật real-time số lượng scan/chỉ số +/- tới các trạm/màn hình liên quan (FR-09).
/// Client (Station.Wpf, tương lai) join 1 group theo đúng WorkStationId của mình qua <see cref="JoinStationGroupAsync"/>
/// rồi lắng nghe method <see cref="ScanRecordedMethodName"/> — <c>ScanHubNotifier</c> (tầng Api) bắn sự kiện
/// này sau khi <c>ScanService</c> lưu thành công 1 lượt scan kết quả OK (US-07 AC2/AC3).
/// </summary>
public class ScanHub : Hub
{
    /// <summary>Tên method SignalR client subscribe để nhận payload <c>ScanResultDto</c> khi có scan OK mới.</summary>
    public const string ScanRecordedMethodName = "ScanRecorded";

    /// <summary>Tên nhóm SignalR ứng với 1 trạm cụ thể — dùng chung giữa Hub (join) và ScanHubNotifier (gửi).</summary>
    public static string GetStationGroupName(int workStationId) => $"station-{workStationId}";

    /// <summary>Client gọi ngay sau khi kết nối để tham gia nhóm nhận cập nhật real-time của đúng trạm mình.</summary>
    public Task JoinStationGroupAsync(int workStationId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GetStationGroupName(workStationId));
}
