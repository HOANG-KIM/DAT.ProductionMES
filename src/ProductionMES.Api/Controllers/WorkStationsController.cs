using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Application.DTOs.WorkStations;
using ProductionMES.Application.Services.WorkStations;

namespace ProductionMES.Api.Controllers;

/// <summary>Quản lý trạm làm việc (US-04/FR-04). Chỉ Admin được quản lý cấu hình trạm (US-22/AC1).</summary>
[ApiController]
[Route("api/v1/work-stations")]
[Authorize(Roles = "Admin")]
public class WorkStationsController : ControllerBase
{
    private readonly IWorkStationService _workStationService;

    public WorkStationsController(IWorkStationService workStationService)
    {
        _workStationService = workStationService;
    }

    /// <summary>Lấy danh sách toàn bộ trạm làm việc (kể cả đã vô hiệu hóa).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkStationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var workStations = await _workStationService.GetAllAsync(cancellationToken);
        return Ok(workStations);
    }

    /// <summary>Lấy chi tiết 1 trạm theo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkStationDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var workStation = await _workStationService.GetByIdAsync(id, cancellationToken);
        return workStation is null ? NotFound() : Ok(workStation);
    }

    /// <summary>Tạo mới 1 trạm, gắn đúng 1 Line và 1 công đoạn (AC1). Kèm thông tin cổng COM nếu dùng Arduino (AC2/AC3).</summary>
    [HttpPost]
    public async Task<ActionResult<WorkStationDto>> Create([FromBody] CreateWorkStationRequest request, CancellationToken cancellationToken)
    {
        var created = await _workStationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật thông tin 1 trạm.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkStationDto>> Update(int id, [FromBody] UpdateWorkStationRequest request, CancellationToken cancellationToken)
    {
        var updated = await _workStationService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Vô hiệu hóa trạm — soft-delete qua cờ hoạt động, không xóa cứng dữ liệu.</summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _workStationService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
