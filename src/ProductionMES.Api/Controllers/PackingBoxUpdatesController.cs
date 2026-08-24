using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.PackingBoxes;
using ProductionMES.Application.Services.PackingBoxes;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Sửa số thùng hiện tại tại "Đóng thùng" (US-25 AC7) — TÁCH RIÊNG khỏi <see cref="PackingBoxesController"/> vì
/// đây là thao tác của NGƯỜI (Tổ trưởng/Admin), cần xác định danh tính — dùng scheme Bearer mặc định + permission
/// động <c>PackingBox.Update</c> (ADR-004, ADR-005 mục 2), cùng pattern <see cref="ReworkUnlockController"/> (US-19).
/// </summary>
[ApiController]
[Route("api/v1/packing-boxes/box-no")]
public class PackingBoxUpdatesController : ControllerBase
{
    private readonly IPackingBoxService _packingBoxService;

    public PackingBoxUpdatesController(IPackingBoxService packingBoxService)
    {
        _packingBoxService = packingBoxService;
    }

    /// <summary>AC7: sửa số thùng hiện tại — chỉ đổi nhãn BoxNo, không đổi số lượng đã quét/mục tiêu.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.PackingBoxUpdate)]
    public async Task<ActionResult<PackingBoxDto>> Update([FromBody] UpdateCurrentBoxNoRequest request, CancellationToken cancellationToken)
    {
        // Người thực hiện THẬT lấy từ claim JWT đã xác thực, KHÔNG nhận trực tiếp từ request body (cùng lý do ScanNgController/ReworkUnlockController).
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var updatedByUserId))
        {
            return Unauthorized();
        }

        var updatedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(updatedByUserName))
        {
            return Unauthorized();
        }

        var result = await _packingBoxService.UpdateCurrentBoxNoAsync(
            request.WorkStationId, request.NewBoxNo, updatedByUserId, updatedByUserName, cancellationToken);
        return Ok(result);
    }
}
