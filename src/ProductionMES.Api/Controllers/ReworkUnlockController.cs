using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.ReworkUnlocks;
using ProductionMES.Application.Services.ReworkUnlocks;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// "Mở khóa rework" cho tem bị NG (US-19 AC2/AC6, FR-19) — TÁCH RIÊNG khỏi <see cref="ScansController"/> (scheme
/// "StationApiKey") vì đây là thao tác của NGƯỜI (Tổ trưởng/Admin), cần xác định danh tính người mở khóa — dùng
/// scheme Bearer mặc định + permission động <c>Scan.ReworkUnlock</c> (ADR-004, ADR-005 mục 2), cùng pattern
/// <see cref="ScanNgController"/> (US-18).
/// </summary>
[ApiController]
[Route("api/v1/scans/rework-unlock")]
public class ReworkUnlockController : ControllerBase
{
    private readonly IReworkUnlockService _reworkUnlockService;

    public ReworkUnlockController(IReworkUnlockService reworkUnlockService)
    {
        _reworkUnlockService = reworkUnlockService;
    }

    /// <summary>
    /// US-19 AC2: mở khóa rework cho 1 tem đang bị khóa tại đúng công đoạn của <see cref="ReworkUnlockRequest.WorkStationId"/>.
    /// 409 (BusinessRuleException) nếu tem hiện KHÔNG đang bị khóa (chưa từng Ng, hoặc đã mở khóa rồi).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ScanReworkUnlock)]
    public async Task<ActionResult<ReworkUnlockDto>> Unlock([FromBody] ReworkUnlockRequest request, CancellationToken cancellationToken)
    {
        // Cùng lý do ScanNgController: người thực hiện THẬT lấy từ claim JWT đã xác thực, KHÔNG nhận trực tiếp từ
        // request body để tránh giả mạo.
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var unlockedByUserId))
        {
            return Unauthorized();
        }

        var unlockedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(unlockedByUserName))
        {
            return Unauthorized();
        }

        var result = await _reworkUnlockService.UnlockAsync(
            request.WorkStationId, request.TagCode, request.Note, unlockedByUserId, unlockedByUserName, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Tra cứu thông tin lỗi NG gần nhất + trạng thái khóa rework hiện tại của 1 tem tại đúng công đoạn của
    /// <paramref name="workStationId"/> (feedback 18/08/2026) — hiển thị tham khảo cho Tổ trưởng TRƯỚC khi bấm
    /// "Xác nhận mở khóa rework", cùng policy quyền với <see cref="Unlock"/> (chỉ ai được mở khóa mới xem được lý
    /// do lỗi).
    /// </summary>
    [HttpGet("status")]
    [Authorize(Policy = PermissionPolicies.ScanReworkUnlock)]
    public async Task<ActionResult<ReworkLockStatusDto>> GetLockStatus(
        [FromQuery] int workStationId, [FromQuery] string tagCode, CancellationToken cancellationToken)
    {
        var result = await _reworkUnlockService.GetLockStatusAsync(workStationId, tagCode, cancellationToken);
        return Ok(result);
    }
}
