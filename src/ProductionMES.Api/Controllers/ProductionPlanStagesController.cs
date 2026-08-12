using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.Services.ProductionPlanStages;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Cấu hình công đoạn áp dụng cho từng kế hoạch sản xuất, kèm trình tự (US-03/FR-03). Phân quyền theo
/// permission động (ADR-004) — mỗi action tự khai báo policy riêng; <see cref="Remove"/> dùng HTTP DELETE thật
/// nên gắn permission <c>Delete</c> (khác các resource còn lại dùng <c>Deactivate</c> soft-delete).
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
}
