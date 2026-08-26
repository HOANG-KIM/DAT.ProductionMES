using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.Scans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Xác nhận LƯU 1 lượt scan bị hệ thống TỰ ĐỘNG từ chối (US-27 AC5/AC6) — TÁCH RIÊNG khỏi <see cref="ScansController"/>
/// (scheme <c>StationApiKey</c>) vì cần xác định danh tính người xác nhận (Bearer mặc định + permission động
/// <c>Scan.ConfirmReject</c>, ADR-004), cùng pattern <see cref="ScanNgController"/> (US-18)/
/// <c>PackingDuplicateConfirmationsController</c> cũ (US-25 AC8, đã bị xóa — thay thế hoàn toàn bởi Controller này,
/// xem US-27 AC12).
/// </summary>
[ApiController]
[Route("api/v1/scans/reject-confirmations")]
public class ScanRejectConfirmationsController : ControllerBase
{
    private readonly IScanService _scanService;

    public ScanRejectConfirmationsController(IScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// US-27 AC5/AC6: lưu 1 bản ghi Scan cho lượt bị từ chối tự động — client gửi lại NGUYÊN VẸN
    /// <see cref="ScanResultDto"/> đã nhận ở <c>POST api/v1/scans</c> (Bước 1, chưa lưu). Luôn trả 201 khi thành
    /// công (bản ghi Scan vừa được tạo mới).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ScanConfirmReject)]
    public async Task<ActionResult<ScanResultDto>> Confirm([FromBody] ConfirmRejectedScanRequest request, CancellationToken cancellationToken)
    {
        // AC6/ADR-005: người xác nhận THẬT lấy từ claim JWT đã xác thực, KHÔNG nhận trực tiếp từ request body để
        // tránh giả mạo (cùng nguyên tắc ScanNgController).
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

        var result = await _scanService.ConfirmRejectedScanAsync(request, confirmedByUserId, confirmedByUserName, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
