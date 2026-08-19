using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Application.Services.ProductionPlans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý thông tin kế hoạch sản xuất (US-05/FR-05, US-06/FR-06). Phân quyền theo permission động (ADR-004) —
/// mỗi action tự khai báo policy riêng (theo seed mặc định, Supervisor/Admin có toàn bộ permission của resource
/// này, nhưng có thể đổi runtime qua UI quản lý permission mà không cần deploy lại).
/// KHÔNG có action Activate/Deactivate ở đây — vòng đời trạng thái theo cặp (Kế hoạch, Công đoạn) nay thuộc
/// <see cref="ProductionPlanStagesController"/> (Apply/Pause/Close, US-05a).
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

    /// <summary>Tạo mới 1 kế hoạch sản xuất, chưa active (AC1). AC7-AC9 (US-21a): có thể bị từ chối 409 nếu Lot hoàn toàn mới mà thiếu "Tổng số lượng Lot", hoặc cần Confirm khi giảm dưới thực tế đã chạy.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ProductionPlanCreate)]
    public async Task<ActionResult<ProductionPlanDto>> Create([FromBody] CreateProductionPlanRequest request, CancellationToken cancellationToken)
    {
        var updatedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        var created = await _productionPlanService.CreateAsync(request, updatedByUserName, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật thông tin 1 kế hoạch (AC4/AC5/AC7-AC9) — có thể bị từ chối nếu chưa Confirm (AC5/AC8).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanUpdate)]
    public async Task<ActionResult<ProductionPlanDto>> Update(int id, [FromBody] UpdateProductionPlanRequest request, CancellationToken cancellationToken)
    {
        var updatedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        var updated = await _productionPlanService.UpdateAsync(id, request, updatedByUserName, cancellationToken);
        return Ok(updated);
    }
}
