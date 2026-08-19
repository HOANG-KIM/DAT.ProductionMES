using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.ReworkUnlocks;

/// <summary>
/// Gọi <c>api/v1/scans/rework-unlock</c> (US-19 AC2) — xác thực bằng Bearer Tổ trưởng, yêu cầu quyền
/// <c>Scan.ReworkUnlock</c>. **18/08/2026 (AC7 — re-auth mỗi lần)**: token gắn tường minh theo từng request qua
/// tham số <c>supervisorAccessToken</c> (KHÔNG còn dùng <c>SupervisorAuthHandler</c>/<c>ISupervisorSessionService</c>
/// dùng chung — cùng idiom <c>ScanApiClient.CreateNgAsync</c> của US-18), vì phiên đăng nhập chung có thể còn hiệu
/// lực cho các chức năng Tổ trưởng khác trong khi chức năng này bắt buộc đăng nhập lại độc lập.
/// </summary>
public interface IReworkUnlockApiClient
{
    Task<ReworkUnlockDto> UnlockAsync(string tagCode, int workStationId, string? note, string supervisorAccessToken, CancellationToken cancellationToken = default);

    /// <summary>Gọi <c>GET api/v1/scans/rework-unlock/status</c> — tra cứu lỗi NG gần nhất + trạng thái khóa hiện tại của 1 tem (feedback 18/08/2026).</summary>
    Task<ReworkLockStatusDto> GetLockStatusAsync(string tagCode, int workStationId, string supervisorAccessToken, CancellationToken cancellationToken = default);
}
