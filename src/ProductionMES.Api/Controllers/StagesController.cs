using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Application.DTOs.Stages;
using ProductionMES.Application.Services.Stages;

namespace ProductionMES.Api.Controllers;

/// <summary>Quản lý danh mục Công đoạn master (US-02/FR-02). Chỉ Admin được quản lý danh mục (US-22/AC1).</summary>
[ApiController]
[Route("api/v1/stages")]
[Authorize(Roles = "Admin")]
public class StagesController : ControllerBase
{
    private readonly IStageService _stageService;

    public StagesController(IStageService stageService)
    {
        _stageService = stageService;
    }

    /// <summary>Lấy danh sách toàn bộ công đoạn (kể cả đã vô hiệu hóa).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StageDto>>> GetAll(CancellationToken cancellationToken)
    {
        var stages = await _stageService.GetAllAsync(cancellationToken);
        return Ok(stages);
    }

    /// <summary>Lấy chi tiết 1 công đoạn theo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StageDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var stage = await _stageService.GetByIdAsync(id, cancellationToken);
        return stage is null ? NotFound() : Ok(stage);
    }

    /// <summary>Tạo mới 1 công đoạn master (AC1).</summary>
    [HttpPost]
    public async Task<ActionResult<StageDto>> Create([FromBody] CreateStageRequest request, CancellationToken cancellationToken)
    {
        var created = await _stageService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật tên/mô tả 1 công đoạn.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<StageDto>> Update(int id, [FromBody] UpdateStageRequest request, CancellationToken cancellationToken)
    {
        var updated = await _stageService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Vô hiệu hóa công đoạn — soft-delete qua cờ hoạt động, không xóa cứng dữ liệu (AC3).</summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _stageService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
