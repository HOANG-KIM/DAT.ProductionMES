using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Application.DTOs.Lines;
using ProductionMES.Application.Services.Lines;

namespace ProductionMES.Api.Controllers;

/// <summary>Quản lý danh mục Line sản xuất (US-01/FR-01). Chỉ Admin được quản lý danh mục (US-22/AC1).</summary>
[ApiController]
[Route("api/v1/lines")]
[Authorize(Roles = "Admin")]
public class LinesController : ControllerBase
{
    private readonly ILineService _lineService;

    public LinesController(ILineService lineService)
    {
        _lineService = lineService;
    }

    /// <summary>Lấy danh sách toàn bộ Line (kể cả Line đã vô hiệu hóa).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LineDto>>> GetAll(CancellationToken cancellationToken)
    {
        var lines = await _lineService.GetAllAsync(cancellationToken);
        return Ok(lines);
    }

    /// <summary>Lấy chi tiết 1 Line theo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LineDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var line = await _lineService.GetByIdAsync(id, cancellationToken);
        return line is null ? NotFound() : Ok(line);
    }

    /// <summary>Tạo mới 1 Line (AC1).</summary>
    [HttpPost]
    public async Task<ActionResult<LineDto>> Create([FromBody] CreateLineRequest request, CancellationToken cancellationToken)
    {
        var created = await _lineService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật tên/mô tả 1 Line (AC2).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LineDto>> Update(int id, [FromBody] UpdateLineRequest request, CancellationToken cancellationToken)
    {
        var updated = await _lineService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Vô hiệu hóa Line — soft-delete qua cờ hoạt động, không xóa cứng dữ liệu (AC3).</summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _lineService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
