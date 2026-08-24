using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.PackingBoxes;
using ProductionMES.Application.Services.PackingBoxes;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Xác nhận đã biết tình huống tem trùng tại "Đóng thùng" (US-25 AC8, mục 6 quy tắc 16 SRS) — TÁCH RIÊNG khỏi
/// <see cref="PackingBoxesController"/> (StationApiKey), dùng scheme Bearer mặc định + permission động
/// <c>PackingBox.ConfirmDuplicate</c>, cùng pattern <see cref="ReworkUnlockController"/>/<see cref="PackingBoxUpdatesController"/>.
/// CHỈ ghi audit (ai đã xử lý) — KHÔNG cộng số lượng vào thùng hiện tại, KHÔNG tạo thêm bản ghi Scan (AC8 "KHÔNG
/// phải ngoại lệ ghi đè của FR-08").
/// </summary>
[ApiController]
[Route("api/v1/packing-boxes/duplicate-confirmations")]
public class PackingDuplicateConfirmationsController : ControllerBase
{
    private readonly IPackingBoxService _packingBoxService;

    public PackingDuplicateConfirmationsController(IPackingBoxService packingBoxService)
    {
        _packingBoxService = packingBoxService;
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.PackingBoxConfirmDuplicate)]
    public async Task<ActionResult<PackingDuplicateConfirmationDto>> Confirm(
        [FromBody] ConfirmPackingDuplicateRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var confirmedByUserId))
        {
            return Unauthorized();
        }

        var confirmedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(confirmedByUserName))
        {
            return Unauthorized();
        }

        var result = await _packingBoxService.ConfirmDuplicateAsync(
            request.WorkStationId, request.TagCode, confirmedByUserId, confirmedByUserName, request.Note, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
