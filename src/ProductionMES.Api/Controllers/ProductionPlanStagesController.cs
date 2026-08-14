using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.Services.ProductionPlanStages;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Cấu hình công đoạn áp dụng cho từng kế hoạch sản xuất, kèm trình tự (US-03/FR-03) VÀ vòng đời trạng thái
/// theo cặp (Kế hoạch, Công đoạn) — Áp dụng/Tạm dừng/Đóng (US-05a/FR-05a). Phân quyền theo permission động
/// (ADR-004) — mỗi action tự khai báo policy riêng; <see cref="Remove"/> dùng HTTP DELETE thật nên gắn
/// permission <c>Delete</c> (khác các resource còn lại dùng <c>Deactivate</c> soft-delete).
/// </summary>
[ApiController]
[Route("api/v1/production-plans/{productionPlanId:int}/stages")]
public class ProductionPlanStagesController : ControllerBase
{
    private readonly IProductionPlanStageService _productionPlanStageService;

    public ProductionPlanStagesController(IProductionPlanStageService productionPlanStageService)
    {
        _productionPlanStageService = productionPlanStageService;
    }

    /// <summary>Lấy danh sách công đoạn (kèm trình tự) đã cấu hình cho kế hoạch, sắp theo SequenceNumber.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageView)]
    public async Task<ActionResult<IReadOnlyList<ProductionPlanStageDto>>> GetAll(int productionPlanId, CancellationToken cancellationToken)
    {
        var items = await _productionPlanStageService.GetByProductionPlanAsync(productionPlanId, cancellationToken);
        return Ok(items);
    }

    /// <summary>Thêm 1 công đoạn từ danh mục master vào kế hoạch (AC1).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageCreate)]
    public async Task<ActionResult<ProductionPlanStageDto>> Add(int productionPlanId, [FromBody] AddStageToProductionPlanRequest request, CancellationToken cancellationToken)
    {
        var created = await _productionPlanStageService.AddAsync(productionPlanId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { productionPlanId }, created);
    }

    /// <summary>Gỡ 1 công đoạn khỏi kế hoạch, tự động điều chỉnh lại trình tự còn lại (AC2).</summary>
    [HttpDelete("{stageId:int}")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageDelete)]
    public async Task<IActionResult> Remove(int productionPlanId, int stageId, CancellationToken cancellationToken)
    {
        await _productionPlanStageService.RemoveAsync(productionPlanId, stageId, cancellationToken);
        return NoContent();
    }

    /// <summary>Sắp xếp lại toàn bộ trình tự công đoạn của kế hoạch — từ chối nếu trùng thứ tự (AC4) hoặc tạo vòng lặp (AC5) (AC3).</summary>
    [HttpPut("reorder")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageUpdate)]
    public async Task<ActionResult<IReadOnlyList<ProductionPlanStageDto>>> Reorder(int productionPlanId, [FromBody] ReorderProductionPlanStageRequest request, CancellationToken cancellationToken)
    {
        var result = await _productionPlanStageService.ReorderAsync(productionPlanId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Áp dụng kế hoạch cho công đoạn này, chuyển Draft/Paused → Running (US-05a AC1) — từ chối nếu (Line, Công
    /// đoạn) đang có 1 kế hoạch khác Running (AC1/AC2) hoặc cặp này đã Completed/Cancelled (AC7).
    /// </summary>
    [HttpPost("{stageId:int}/apply")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageApply)]
    public async Task<ActionResult<ProductionPlanStageDto>> Apply(int productionPlanId, int stageId, CancellationToken cancellationToken)
    {
        var result = await _productionPlanStageService.ApplyAsync(productionPlanId, stageId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Tạm dừng kế hoạch tại công đoạn này, giữ nguyên tiến độ (US-05a AC3).</summary>
    [HttpPost("{stageId:int}/pause")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStagePause)]
    public async Task<ActionResult<ProductionPlanStageDto>> Pause(int productionPlanId, int stageId, CancellationToken cancellationToken)
    {
        var result = await _productionPlanStageService.PauseAsync(productionPlanId, stageId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Đóng kế hoạch tại công đoạn này, đóng sớm thủ công → Cancelled (US-05a AC6) — yêu cầu Confirm nếu chưa đủ số lượng.</summary>
    [HttpPost("{stageId:int}/close")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageClose)]
    public async Task<ActionResult<ProductionPlanStageDto>> Close(int productionPlanId, int stageId, [FromBody] CloseProductionPlanStageRequest request, CancellationToken cancellationToken)
    {
        var result = await _productionPlanStageService.CloseAsync(productionPlanId, stageId, request, cancellationToken);
        return Ok(result);
    }
}
