using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authentication;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Testers;
using ProductionMES.Application.Services.Testers;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Máy thông điện ESP32 theo trạm (US-11, US-14): đọc trạng thái, đổi công tắc "Đang sử dụng máy thông điện",
/// ghi event log sự kiện máy. Trộn 2 scheme theo từng action, cùng cách <c>ScansController</c>/<c>ScanNgController</c>
/// (ADR-005): đọc trạng thái/ghi event do "trạm" thực hiện (StationApiKey); đổi công tắc do "người" thực hiện
/// (Bearer Tổ trưởng + permission <c>TesterMode.Change</c>).
/// </summary>
[ApiController]
[Route("api/v1/tester")]
public class TesterController : ControllerBase
{
    private readonly ITesterStationService _testerStationService;

    public TesterController(ITesterStationService testerStationService)
    {
        _testerStationService = testerStationService;
    }

    /// <summary>
    /// US-11 AC10: trạm đọc trạng thái máy thông điện CỦA CHÍNH NÓ (<c>UseArduino</c> + công tắc) — WorkStationId lấy
    /// từ claim API key, không nhận từ client.
    /// </summary>
    [HttpGet("station-state")]
    [Authorize(AuthenticationSchemes = StationApiKeyDefaults.AuthenticationScheme)]
    public async Task<ActionResult<TesterStationStateDto>> GetStationState(CancellationToken cancellationToken)
    {
        var workStationIdClaim = User.FindFirst(StationApiKeyDefaults.WorkStationIdClaimType)?.Value;
        if (!int.TryParse(workStationIdClaim, out var workStationId))
        {
            return Unauthorized();
        }

        return Ok(await _testerStationService.GetStateAsync(workStationId, cancellationToken));
    }

    /// <summary>
    /// US-11 AC4/AC5: đổi công tắc "Đang sử dụng máy thông điện" — người duyệt lấy từ claim Bearer (không tin body).
    /// Trạm không có máy / thiếu lý do -> 409 / 400, công tắc giữ nguyên, không ghi log.
    /// </summary>
    [HttpPut("enabled")]
    [Authorize(Policy = PermissionPolicies.TesterModeChange)]
    public async Task<ActionResult<TesterStationStateDto>> SetEnabled([FromBody] SetTesterEnabledRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var approvedByUserId))
        {
            return Unauthorized();
        }

        var approvedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(approvedByUserName))
        {
            return Unauthorized();
        }

        var state = await _testerStationService.SetEnabledAsync(
            request.WorkStationId, request.Enabled, request.Reason, approvedByUserId, approvedByUserName, cancellationToken);
        return Ok(state);
    }
}
