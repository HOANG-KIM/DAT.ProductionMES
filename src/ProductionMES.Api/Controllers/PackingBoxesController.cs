using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authentication;
using ProductionMES.Application.DTOs.PackingBoxes;
using ProductionMES.Application.Services.PackingBoxes;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Đọc trạng thái đóng thùng + nhập số thùng bắt đầu + tải tem thùng in tại "Đóng thùng" (US-25). Xác thực bằng
/// scheme "StationApiKey" (ADR-005, cùng <see cref="ScansController"/>) — Operator KHÔNG cần đăng nhập cá nhân
/// cho các thao tác này (khác <see cref="PackingBoxUpdatesController"/>/<see cref="PackingDuplicateConfirmationsController"/>
/// — AC7/AC8, bắt buộc Supervisor).
/// </summary>
[ApiController]
[Route("api/v1/packing-boxes")]
[Authorize(AuthenticationSchemes = StationApiKeyDefaults.AuthenticationScheme)]
public class PackingBoxesController : ControllerBase
{
    /// <summary>Content-Type chuẩn cho file .xlsx — cùng hằng số dùng ở <c>PackingModelConfigsController</c>/<c>LotReportsController</c>.</summary>
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IPackingBoxService _packingBoxService;

    public PackingBoxesController(IPackingBoxService packingBoxService)
    {
        _packingBoxService = packingBoxService;
    }

    /// <summary>AC5/AC6/AC9: trạng thái đóng thùng hiện tại của trạm — gọi khi khởi động/quay lại màn hình để tự khôi phục đúng BoxNo + số lượng đang dở.</summary>
    [HttpGet("state")]
    public async Task<ActionResult<PackingBoxStateDto>> GetState(CancellationToken cancellationToken)
    {
        if (!TryGetWorkStationId(out var workStationId))
        {
            return Unauthorized();
        }

        var state = await _packingBoxService.GetStateAsync(workStationId, cancellationToken);
        return Ok(state);
    }

    /// <summary>AC5: nhập số thùng bắt đầu — chỉ cho phép lần đầu đóng thùng của 1 kế hoạch.</summary>
    [HttpPost("starting-box-no")]
    public async Task<ActionResult<PackingBoxDto>> SetStartingBoxNo([FromBody] SetStartingBoxNoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetWorkStationId(out var workStationId))
        {
            return Unauthorized();
        }

        var box = await _packingBoxService.SetStartingBoxNoAsync(workStationId, request.StartingBoxNo, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, box);
    }

    /// <summary>AC4/AC13: tải file tem thùng đã merge dữ liệu — dùng cho in tự động lẫn "In lại" thủ công. Trạm nào cũng tải được theo Id thùng (không giới hạn đúng trạm đã mở thùng đó — nội bộ, tin cậy được).</summary>
    [HttpGet("{id:int}/label")]
    public async Task<IActionResult> DownloadLabel(int id, CancellationToken cancellationToken)
    {
        var (content, fileName) = await _packingBoxService.GenerateLabelAsync(id, cancellationToken);
        return File(content, XlsxContentType, fileName);
    }

    /// <summary>ADR-005: WorkStationId THẬT lấy từ danh tính trạm đã xác thực (claim), không tin request body.</summary>
    private bool TryGetWorkStationId(out int workStationId)
    {
        var workStationIdClaim = User.FindFirst(StationApiKeyDefaults.WorkStationIdClaimType)?.Value;
        return int.TryParse(workStationIdClaim, out workStationId);
    }
}
