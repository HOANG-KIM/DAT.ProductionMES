using ProductionMES.Application.DTOs.Common;
using ProductionMES.Application.DTOs.Scans;

namespace ProductionMES.Application.Services.Scans;

public interface IScanService
{
    /// <summary>
    /// Ghi nhận 1 lượt scan tem tại trạm (US-07/US-08, FR-07/FR-08). Luôn lưu lại bản ghi Scan bất kể kết quả
    /// (FR-10) — kể cả khi bị từ chối do trùng tem/chưa qua công đoạn liền trước.
    /// </summary>
    /// <param name="workStationId">
    /// Id trạm THẬT lấy từ danh tính đã xác thực (claim), không phải từ request body — nguồn thật duy nhất
    /// dùng để xác định Line/Stage của lượt scan (ADR-005).
    /// </param>
    /// <param name="tagCode">Mã tem được scan.</param>
    Task<ScanResultDto> CreateAsync(int workStationId, string tagCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tra cứu lịch sử scan (US-10 AC2/AC3) — theo mã tem, hoặc theo trạm/Line/khoảng thời gian, có thể kết hợp
    /// nhiều điều kiện cùng lúc (AND). Kết quả luôn sắp theo <c>ScannedAtUtc</c> tăng dần (AC2 "sắp xếp theo thời
    /// gian") và phân trang theo `Documents/API-Conventions.md` mục 9.
    /// </summary>
    Task<PagedResult<ScanHistoryItemDto>> GetHistoryAsync(ScanHistoryQuery query, CancellationToken cancellationToken = default);
}
