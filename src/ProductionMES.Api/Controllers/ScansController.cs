using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authentication;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.Scans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Ghi nhận lượt scan tem tại trạm (US-07/US-08, FR-07/FR-08). Xác thực bằng scheme riêng "StationApiKey"
/// (ADR-005) — KHÔNG đi qua hệ permission Resource.Action động (ADR-004), vì không có User nào gắn với request
/// dùng scheme này.
/// </summary>
[ApiController]
[Route("api/v1/scans")]
[Authorize(AuthenticationSchemes = StationApiKeyDefaults.AuthenticationScheme)]
public class ScansController : ControllerBase
{
    private readonly IScanService _scanService;

    public ScansController(IScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// Ghi nhận 1 lượt scan. Luôn trả 201 kèm kết quả (kể cả khi bị từ chối — DuplicateTag/PreviousStageNotPassed
    /// là kết quả nghiệp vụ hợp lệ theo FR-08, không phải lỗi HTTP) vì bản ghi Scan luôn được tạo mới (FR-10).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ScanResultDto>> Create([FromBody] CreateScanRequest request, CancellationToken cancellationToken)
    {
        // ADR-005: WorkStationId THẬT lấy từ danh tính trạm đã xác thực (claim), không tin trực tiếp
        // request.WorkStationId dù StationApiKeyAuthenticationHandler đã đối chiếu ở tầng auth (AC6).
        var workStationIdClaim = User.FindFirst(StationApiKeyDefaults.WorkStationIdClaimType)?.Value;
        if (!int.TryParse(workStationIdClaim, out var workStationId))
        {
            return Unauthorized();
        }

        var result = await _scanService.CreateAsync(workStationId, request.TagCode, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
