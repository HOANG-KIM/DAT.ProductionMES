using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.StationApiKeys;
using ProductionMES.Application.Services.StationApiKeys;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý API Key theo trạm (US-04a, ADR-005) — cấp/xem/thu hồi/cấp lại, dùng bởi Admin ở web-admin (không
/// phải Station.Wpf). Route dùng số ít <c>api-key</c> (khác quy ước danh từ số nhiều chung của
/// Documents/API-Conventions.md) vì đại diện đúng 1 khái niệm "key hiện tại của trạm", tương tự cách
/// <c>PUT api/v1/users/{id}/role</c> dùng số ít cho sub-resource 1-1. Phân quyền theo permission động (ADR-004)
/// — hoàn toàn tách biệt khỏi <c>AuthenticationScheme "StationApiKey"</c> mà chính API key này dùng để xác thực
/// (ADR-005 dòng 28).
/// </summary>
[ApiController]
[Route("api/v1/work-stations/{workStationId:int}/api-key")]
public class StationApiKeysController : ControllerBase
{
    private readonly IStationApiKeyService _stationApiKeyService;

    public StationApiKeysController(IStationApiKeyService stationApiKeyService)
    {
        _stationApiKeyService = stationApiKeyService;
    }

    /// <summary>Lấy metadata key mới nhất của trạm (Active hoặc Revoked) — không lộ giá trị thô (AC2).</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.StationApiKeyView)]
    public async Task<ActionResult<StationApiKeyDto>> GetCurrent(int workStationId, CancellationToken cancellationToken)
    {
        var current = await _stationApiKeyService.GetCurrentAsync(workStationId, cancellationToken);
        return current is null ? NotFound() : Ok(current);
    }

    /// <summary>Cấp API Key mới cho trạm — trả giá trị thô đúng 1 lần duy nhất (AC1).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.StationApiKeyCreate)]
    public async Task<ActionResult<IssuedStationApiKeyDto>> Issue(int workStationId, CancellationToken cancellationToken)
    {
        var issued = await _stationApiKeyService.IssueAsync(workStationId, cancellationToken);
        return CreatedAtAction(nameof(GetCurrent), new { workStationId }, issued);
    }

    /// <summary>Thu hồi key Active hiện tại của trạm (AC3).</summary>
    [HttpPost("revoke")]
    [Authorize(Policy = PermissionPolicies.StationApiKeyDeactivate)]
    public async Task<IActionResult> Revoke(int workStationId, CancellationToken cancellationToken)
    {
        await _stationApiKeyService.RevokeAsync(workStationId, cancellationToken);
        return NoContent();
    }

    /// <summary>Xoay vòng: thu hồi key cũ (nếu có) + cấp key mới, trả giá trị thô đúng 1 lần duy nhất (AC4).</summary>
    [HttpPost("reissue")]
    [Authorize(Policy = PermissionPolicies.StationApiKeyUpdate)]
    public async Task<ActionResult<IssuedStationApiKeyDto>> Reissue(int workStationId, CancellationToken cancellationToken)
    {
        var issued = await _stationApiKeyService.ReissueAsync(workStationId, cancellationToken);
        return Ok(issued);
    }
}
