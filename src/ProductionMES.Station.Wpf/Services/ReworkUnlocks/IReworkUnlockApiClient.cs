using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.ReworkUnlocks;

/// <summary>
/// Gọi <c>api/v1/scans/rework-unlock</c> (US-19 AC2) — xác thực bằng Bearer Tổ trưởng (<c>SupervisorAuthHandler</c>,
/// ADR-005 mục 2), yêu cầu quyền <c>Scan.ReworkUnlock</c>.
/// </summary>
public interface IReworkUnlockApiClient
{
    Task<ReworkUnlockDto> UnlockAsync(string tagCode, int workStationId, string? note, CancellationToken cancellationToken = default);

    /// <summary>Gọi <c>GET api/v1/scans/rework-unlock/status</c> — tra cứu lỗi NG gần nhất + trạng thái khóa hiện tại của 1 tem (feedback 18/08/2026).</summary>
    Task<ReworkLockStatusDto> GetLockStatusAsync(string tagCode, int workStationId, CancellationToken cancellationToken = default);
}
