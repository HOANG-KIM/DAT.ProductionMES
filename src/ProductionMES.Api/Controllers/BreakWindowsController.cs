using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.BreakWindows;
using ProductionMES.Application.Services.BreakWindows;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý khung giờ nghỉ theo Line (US-01a/FR-01/FR-09a). Phân quyền theo permission động (ADR-004) — mỗi
/// action tự khai báo policy riêng. Dùng HTTP DELETE thật (không phải deactivate) vì BreakWindow là bản ghi
/// cấu hình thuần túy, không có ý nghĩa lịch sử độc lập (cùng nhóm với ProductionPlanStage).
/// </summary>
[ApiController]
[Route("api/v1/lines/{lineId:int}/break-windows")]
public class BreakWindowsController : ControllerBase
{
    private readonly IBreakWindowService _breakWindowService;

    public BreakWindowsController(IBreakWindowService breakWindowService)
    {
        _breakWindowService = breakWindowService;
    }

    /// <summary>Lấy danh sách khung giờ nghỉ đã cấu hình cho Line (có thể rỗng — AC4).</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.BreakWindowView)]
    public async Task<ActionResult<IReadOnlyList<BreakWindowDto>>> GetAll(int lineId, CancellationToken cancellationToken)
    {
        var items = await _breakWindowService.GetByLineAsync(lineId, cancellationToken);
        return Ok(items);
    }

    /// <summary>Thêm 1 khung giờ nghỉ cho Line (AC1/AC2).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.BreakWindowCreate)]
    public async Task<ActionResult<BreakWindowDto>> Create(int lineId, [FromBody] CreateBreakWindowRequest request, CancellationToken cancellationToken)
    {
        var created = await _breakWindowService.CreateAsync(lineId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { lineId }, created);
    }

    /// <summary>Sửa giờ bắt đầu/kết thúc/ghi chú của 1 khung giờ nghỉ (AC3).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionPolicies.BreakWindowUpdate)]
    public async Task<ActionResult<BreakWindowDto>> Update(int lineId, int id, [FromBody] UpdateBreakWindowRequest request, CancellationToken cancellationToken)
    {
        var updated = await _breakWindowService.UpdateAsync(lineId, id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Xóa 1 khung giờ nghỉ (AC3).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermissionPolicies.BreakWindowDelete)]
    public async Task<IActionResult> Delete(int lineId, int id, CancellationToken cancellationToken)
    {
        await _breakWindowService.DeleteAsync(lineId, id, cancellationToken);
        return NoContent();
    }
}
