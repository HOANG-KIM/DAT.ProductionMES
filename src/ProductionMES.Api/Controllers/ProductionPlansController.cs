using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Application.Services.ProductionPlans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý kế hoạch sản xuất (US-05/FR-05, US-06/FR-06). Theo SRS mục 2.2, quyền cấu hình kế hoạch thuộc
/// Tổ trưởng/Quản lý chuyền hoặc Admin.
/// </summary>
[ApiController]
[Route("api/v1/production-plans")]
[Authorize(Roles = "Supervisor,Admin")]
public class ProductionPlansController : ControllerBase
{
    private readonly IProductionPlanService _productionPlanService;

    public ProductionPlansController(IProductionPlanService productionPlanService)
    {
        _productionPlanService = productionPlanService;
    }

    /// <summary>Lấy danh sách toàn bộ kế hoạch sản xuất.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductionPlanDto>>> GetAll(CancellationToken cancellationToken)
    {
        var productionPlans = await _productionPlanService.GetAllAsync(cancellationToken);
        return Ok(productionPlans);
    }

    /// <summary>Lấy chi tiết 1 kế hoạch theo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductionPlanDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var productionPlan = await _productionPlanService.GetByIdAsync(id, cancellationToken);
        return productionPlan is null ? NotFound() : Ok(productionPlan);
    }

    /// <summary>Tạo mới 1 kế hoạch sản xuất, chưa active (AC1).</summary>
    [HttpPost]
    public async Task<ActionResult<ProductionPlanDto>> Create([FromBody] CreateProductionPlanRequest request, CancellationToken cancellationToken)
    {
        var created = await _productionPlanService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật thông tin 1 kế hoạch (AC3).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductionPlanDto>> Update(int id, [FromBody] UpdateProductionPlanRequest request, CancellationToken cancellationToken)
    {
        var updated = await _productionPlanService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Kích hoạt kế hoạch — từ chối nếu Line đã có kế hoạch active khác (AC2).</summary>
    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<ProductionPlanDto>> Activate(int id, CancellationToken cancellationToken)
    {
        var activated = await _productionPlanService.ActivateAsync(id, cancellationToken);
        return Ok(activated);
    }

    /// <summary>Kết thúc (ngưng active) 1 kế hoạch.</summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _productionPlanService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
