using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.Services.ProductionPlanStages;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Vòng đời trạng thái theo cặp (Kế hoạch, Công đoạn) — Áp dụng/Tạm dừng/Đóng (US-05a/FR-05a). Trình tự công
/// đoạn (US-03/FR-03) KHÔNG còn cấu hình ở đây — xem <c>LineStageSequencesController</c>
/// (<c>api/v1/lines/{lineId}/stage-sequence</c>). Phân quyền theo permission động (ADR-004) — mỗi action tự
/// khai báo policy riêng.
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

    /// <summary>Lấy danh sách công đoạn (kèm trình tự, suy từ trình tự cấu hình của Line) áp dụng cho kế hoạch, sắp theo SequenceNumber.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageView)]
    public async Task<ActionResult<IReadOnlyList<ProductionPlanStageDto>>> GetAll(int productionPlanId, CancellationToken cancellationToken)
    {
        var items = await _productionPlanStageService.GetByProductionPlanAsync(productionPlanId, cancellationToken);
        return Ok(items);
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

/// <summary>
/// Truy vấn kế hoạch sản xuất theo (Line, Công đoạn) — phục vụ màn hình "Chọn kế hoạch" (US-05b), nơi Tổ trưởng
/// chọn 1 Công đoạn rồi xem danh sách mọi kế hoạch đã cấu hình áp dụng cho đúng cặp đó. Route phẳng (không lồng
/// theo <c>productionPlanId</c>) vì truy vấn xuyên nhiều kế hoạch, không thuộc về 1 kế hoạch cụ thể.
/// </summary>
[ApiController]
[Route("api/v1/production-plan-stages")]
public class ProductionPlanStageSelectionController : ControllerBase
{
    private readonly IProductionPlanStageService _productionPlanStageService;

    public ProductionPlanStageSelectionController(IProductionPlanStageService productionPlanStageService)
    {
        _productionPlanStageService = productionPlanStageService;
    }

    /// <summary>
    /// Danh sách kế hoạch áp dụng cho (Line, Công đoạn) (US-05b AC2), kèm trạng thái/tiến độ động (US-05a AC3/AC4).
    /// Mặc định ẩn các cặp đã Completed/Cancelled (US-05a AC7) — truyền <paramref name="includeClosed"/> = true để xem lại.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageView)]
    public async Task<ActionResult<IReadOnlyList<ProductionPlanStageSelectionDto>>> GetByLineAndStage(
        [FromQuery] int lineId, [FromQuery] int stageId, [FromQuery] bool includeClosed, CancellationToken cancellationToken)
    {
        var items = await _productionPlanStageService.GetByLineAndStageAsync(lineId, stageId, includeClosed, cancellationToken);
        return Ok(items);
    }
}
