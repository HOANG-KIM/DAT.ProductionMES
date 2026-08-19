using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Common;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.Scans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Tra cứu lịch sử scan (US-10 AC2/AC3, FR-10). Tách riêng khỏi <see cref="ScansController"/> (endpoint ghi
/// scan, dùng scheme <c>StationApiKey</c> theo ADR-005) vì đây là màn hình dành cho Tổ trưởng/Admin/Ban quản lý
/// đăng nhập cá nhân — xác thực bằng scheme mặc định (Bearer/cookie, ADR-003) + permission động (ADR-004), KHÔNG
/// dùng API Key theo trạm.
/// </summary>
[ApiController]
[Route("api/v1/scans/history")]
public class ScanHistoryController : ControllerBase
{
    private readonly IScanService _scanService;

    public ScanHistoryController(IScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// Tra cứu lịch sử scan theo mã tem (AC2), hoặc theo trạm/Line/khoảng thời gian (AC3) — mọi filter đều tùy
    /// chọn và kết hợp AND (vd truyền cả <paramref name="workStationId"/> lẫn <paramref name="from"/>/
    /// <paramref name="to"/>). Kết quả sắp theo thời gian scan tăng dần, phân trang theo
    /// `Documents/API-Conventions.md` mục 9.
    /// </summary>
    /// <param name="tagCode">AC2 — mã tem cần tra cứu (so khớp tuyệt đối).</param>
    /// <param name="workStationId">AC3 — lọc theo trạm làm việc.</param>
    /// <param name="lineId">AC3 — lọc theo Line.</param>
    /// <param name="stageId">Mở rộng 18/08/2026 (US-21 AC7) — lọc theo công đoạn.</param>
    /// <param name="lot">Mở rộng 18/08/2026 (US-21 AC7) — lọc theo Lot.</param>
    /// <param name="model">Mở rộng 18/08/2026 (US-21 AC6) — lọc theo Model.</param>
    /// <param name="customer">Mở rộng 18/08/2026 (US-21 AC6) — lọc theo Khách hàng.</param>
    /// <param name="revision">Mở rộng 18/08/2026 (US-21 AC6) — lọc theo Revision.</param>
    /// <param name="from">AC3 — cận dưới (bao gồm) của <c>ScannedAtUtc</c>, dạng UTC ISO 8601.</param>
    /// <param name="to">AC3 — cận trên (bao gồm) của <c>ScannedAtUtc</c>, dạng UTC ISO 8601.</param>
    /// <param name="page">Trang hiện tại, bắt đầu từ 1 (mặc định 1 nếu không truyền/không hợp lệ).</param>
    /// <param name="pageSize">Số bản ghi/trang (mặc định 20, tối đa 200 — vượt quá sẽ tự chỉnh về mặc định).</param>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ScanView)]
    public async Task<ActionResult<PagedResult<ScanHistoryItemDto>>> GetHistory(
        [FromQuery] string? tagCode,
        [FromQuery] int? workStationId,
        [FromQuery] int? lineId,
        [FromQuery] int? stageId,
        [FromQuery] string? lot,
        [FromQuery] string? model,
        [FromQuery] string? customer,
        [FromQuery] string? revision,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ScanHistoryQuery
        {
            TagCode = tagCode,
            WorkStationId = workStationId,
            LineId = lineId,
            StageId = stageId,
            Lot = lot,
            Model = model,
            Customer = customer,
            Revision = revision,
            FromUtc = from,
            ToUtc = to,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _scanService.GetHistoryAsync(query, cancellationToken);
        return Ok(result);
    }
}
