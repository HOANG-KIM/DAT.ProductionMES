using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.WorkStations;
using ProductionMES.Application.Services.WorkStations;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý trạm làm việc (US-04/FR-04). Phân quyền theo permission động (ADR-004) — mỗi action tự khai báo
/// policy riêng, không còn 1 role hardcode chung cho cả Controller.
/// </summary>
[ApiController]
[Route("api/v1/work-stations")]
public class WorkStationsController : ControllerBase
{
    private readonly IWorkStationService _workStationService;

    public WorkStationsController(IWorkStationService workStationService)
    {
        _workStationService = workStationService;
    }

    /// <summary>Lấy danh sách toàn bộ trạm làm việc (kể cả đã vô hiệu hóa).</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.WorkStationView)]
    public async Task<ActionResult<IReadOnlyList<WorkStationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var workStations = await _workStationService.GetAllAsync(cancellationToken);
        return Ok(workStations);
    }

    /// <summary>Lấy chi tiết 1 trạm theo Id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionPolicies.WorkStationView)]
    public async Task<ActionResult<WorkStationDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var workStation = await _workStationService.GetByIdAsync(id, cancellationToken);
        return workStation is null ? NotFound() : Ok(workStation);
    }

    /// <summary>Tạo mới 1 trạm, gắn đúng 1 Line và 1 công đoạn (AC1). Kèm thông tin cổng COM nếu dùng Arduino (AC2/AC3).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.WorkStationCreate)]
    public async Task<ActionResult<WorkStationDto>> Create([FromBody] CreateWorkStationRequest request, CancellationToken cancellationToken)
    {
        var created = await _workStationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật thông tin 1 trạm.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionPolicies.WorkStationUpdate)]
    public async Task<ActionResult<WorkStationDto>> Update(int id, [FromBody] UpdateWorkStationRequest request, CancellationToken cancellationToken)
    {
        var updated = await _workStationService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Vô hiệu hóa trạm — soft-delete qua cờ hoạt động, không xóa cứng dữ liệu.</summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = PermissionPolicies.WorkStationDeactivate)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _workStationService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
