using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Application.Services.ProductionPlans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý kế hoạch sản xuất (US-05/FR-05, US-06/FR-06). Phân quyền theo permission động (ADR-004) — mỗi
/// action tự khai báo policy riêng (theo seed mặc định, Supervisor/Admin có toàn bộ permission của resource
/// này, nhưng có thể đổi runtime qua UI quản lý permission mà không cần deploy lại).
/// </summary>
[ApiController]
[Route("api/v1/production-plans")]
public class ProductionPlansController : ControllerBase
{
    private readonly IProductionPlanService _productionPlanService;

    public ProductionPlansController(IProductionPlanService productionPlanService)
    {
        _productionPlanService = productionPlanService;
    }

    /// <summary>Lấy danh sách toàn bộ kế hoạch sản xuất.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ProductionPlanView)]
    public async Task<ActionResult<IReadOnlyList<ProductionPlanDto>>> GetAll(CancellationToken cancellationToken)
    {
        var productionPlans = await _productionPlanService.GetAllAsync(cancellationToken);
        return Ok(productionPlans);
    }

    /// <summary>Lấy chi tiết 1 kế hoạch theo Id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanView)]
    public async Task<ActionResult<ProductionPlanDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var productionPlan = await _productionPlanService.GetByIdAsync(id, cancellationToken);
        return productionPlan is null ? NotFound() : Ok(productionPlan);
    }

    /// <summary>Tạo mới 1 kế hoạch sản xuất, chưa active (AC1).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ProductionPlanCreate)]
    public async Task<ActionResult<ProductionPlanDto>> Create([FromBody] CreateProductionPlanRequest request, CancellationToken cancellationToken)
    {
        var created = await _productionPlanService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật thông tin 1 kế hoạch (AC3).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanUpdate)]
    public async Task<ActionResult<ProductionPlanDto>> Update(int id, [FromBody] UpdateProductionPlanRequest request, CancellationToken cancellationToken)
    {
        var updated = await _productionPlanService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Kích hoạt kế hoạch — từ chối nếu Line đã có kế hoạch active khác (AC2).</summary>
    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanActivate)]
    public async Task<ActionResult<ProductionPlanDto>> Activate(int id, CancellationToken cancellationToken)
    {
        var activated = await _productionPlanService.ActivateAsync(id, cancellationToken);
        return Ok(activated);
    }

    /// <summary>Kết thúc (ngưng active) 1 kế hoạch.</summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanDeactivate)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _productionPlanService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
