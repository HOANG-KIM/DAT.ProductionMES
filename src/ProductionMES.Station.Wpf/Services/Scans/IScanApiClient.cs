using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Scans;

/// <summary>Gọi <c>api/v1/scans</c> (US-07/US-08) — xác thực bằng API Key theo trạm (ADR-005, scheme <c>StationApiKey</c>), không cần đăng nhập Supervisor.</summary>
public interface IScanApiClient
{
    /// <summary>
    /// Ghi nhận 1 lượt scan. Luôn trả về <see cref="ScanResultDto"/> (kể cả khi bị từ chối — DuplicateTag/
    /// PreviousStageNotPassed là kết quả nghiệp vụ hợp lệ, không phải lỗi HTTP, xem FR-08/FR-10).
    /// </summary>
    Task<ScanResultDto> CreateAsync(string tagCode, int workStationId, CancellationToken cancellationToken = default);
}
