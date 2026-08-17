using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authentication;
using ProductionMES.Application.DTOs.AndonBoard;
using ProductionMES.Application.Services.AndonBoard;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Dữ liệu bảng PLAN/ACTUAL/BALANCE theo mốc giờ tại màn hình trạm (US-09, FR-09/FR-09a). Xác thực bằng scheme
/// riêng "StationApiKey" (ADR-005) — cùng nguyên tắc với <c>ScansController</c>: KHÔNG đi qua hệ permission
/// Resource.Action động (ADR-004), vì không có User nào gắn với request dùng scheme này.
/// </summary>
[ApiController]
[Route("api/v1/andon-board")]
[Authorize(AuthenticationSchemes = StationApiKeyDefaults.AuthenticationScheme)]
public class AndonBoardController : ControllerBase
{
    private readonly IAndonBoardService _andonBoardService;

    public AndonBoardController(IAndonBoardService andonBoardService)
    {
        _andonBoardService = andonBoardService;
    }

    /// <summary>
    /// Lấy dữ liệu bảng cho đúng trạm đã xác thực. Luôn trả 200 (kể cả khi (Line, Công đoạn) của trạm chưa có kế
    /// hoạch nào đang Running — <see cref="AndonBoardDto.HasActivePlan"/> = false) vì đây là endpoint chỉ hiển
    /// thị, khác <c>POST api/v1/scans</c> (hành động ghi nhận, từ chối bằng 409 khi không có kế hoạch active).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AndonBoardDto>> Get(CancellationToken cancellationToken)
    {
        // ADR-005: WorkStationId THẬT lấy từ danh tính trạm đã xác thực (claim) — cùng cách ScansController đang làm.
        var workStationIdClaim = User.FindFirst(StationApiKeyDefaults.WorkStationIdClaimType)?.Value;
        if (!int.TryParse(workStationIdClaim, out var workStationId))
        {
            return Unauthorized();
        }

        var result = await _andonBoardService.GetForWorkStationAsync(workStationId, cancellationToken);
        return Ok(result);
    }
}
