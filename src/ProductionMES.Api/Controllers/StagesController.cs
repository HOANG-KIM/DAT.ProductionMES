using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Stages;
using ProductionMES.Application.Services.Stages;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý danh mục Công đoạn master (US-02/FR-02). Phân quyền theo permission động (ADR-004) — mỗi action tự
/// khai báo policy riêng, không còn 1 role hardcode chung cho cả Controller.
/// </summary>
[ApiController]
[Route("api/v1/stages")]
public class StagesController : ControllerBase
{
    private readonly IStageService _stageService;

    public StagesController(IStageService stageService)
    {
        _stageService = stageService;
    }

    /// <summary>Lấy danh sách toàn bộ công đoạn (kể cả đã vô hiệu hóa).</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.StageView)]
    public async Task<ActionResult<IReadOnlyList<StageDto>>> GetAll(CancellationToken cancellationToken)
    {
        var stages = await _stageService.GetAllAsync(cancellationToken);
        return Ok(stages);
    }

    /// <summary>Lấy chi tiết 1 công đoạn theo Id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionPolicies.StageView)]
    public async Task<ActionResult<StageDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var stage = await _stageService.GetByIdAsync(id, cancellationToken);
        return stage is null ? NotFound() : Ok(stage);
    }

    /// <summary>Tạo mới 1 công đoạn master (AC1).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.StageCreate)]
    public async Task<ActionResult<StageDto>> Create([FromBody] CreateStageRequest request, CancellationToken cancellationToken)
    {
        var created = await _stageService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật tên/mô tả 1 công đoạn.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionPolicies.StageUpdate)]
    public async Task<ActionResult<StageDto>> Update(int id, [FromBody] UpdateStageRequest request, CancellationToken cancellationToken)
    {
        var updated = await _stageService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Vô hiệu hóa công đoạn — soft-delete qua cờ hoạt động, không xóa cứng dữ liệu (AC3).</summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = PermissionPolicies.StageDeactivate)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _stageService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
