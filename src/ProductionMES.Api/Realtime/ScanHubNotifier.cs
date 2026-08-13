using Microsoft.AspNetCore.SignalR;
using ProductionMES.Api.Hubs;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.Scans;

namespace ProductionMES.Api.Realtime;

/// <summary>
/// Implementation IScanNotifier (US-07 AC2/AC3, FR-09) — bắn sự kiện qua <see cref="ScanHub"/> tới đúng nhóm
/// của trạm gửi lượt scan. Đặt ở tầng Api vì <see cref="ScanHub"/>/<see cref="IHubContext{THub}"/> chỉ tồn tại
/// ở đây; Application chỉ biết tới abstraction <see cref="IScanNotifier"/>.
/// </summary>
public class ScanHubNotifier : IScanNotifier
{
    private readonly IHubContext<ScanHub> _hubContext;

    public ScanHubNotifier(IHubContext<ScanHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyScanRecordedAsync(int workStationId, ScanResultDto scanResult, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group(ScanHub.GetStationGroupName(workStationId))
            .SendAsync(ScanHub.ScanRecordedMethodName, scanResult, cancellationToken);
}
