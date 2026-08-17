using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.LineStageSequences;
using ProductionMES.Application.Services.LineStageSequences;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Cấu hình trình tự công đoạn của 1 Line sản xuất (US-03/FR-03) — thiết lập 1 lần, dùng chung cho mọi kế hoạch
/// chạy trên Line đó. Dùng lại policy có sẵn của <c>ProductionPlanStage</c> (cùng resource family với vòng đời
/// PlanStage, tránh phải seed permission mới trong DB).
/// </summary>
[ApiController]
[Route("api/v1/lines/{lineId:int}/stage-sequence")]
public class LineStageSequencesController : ControllerBase
{
    private readonly ILineStageSequenceService _lineStageSequenceService;

    public LineStageSequencesController(ILineStageSequenceService lineStageSequenceService)
    {
        _lineStageSequenceService = lineStageSequenceService;
    }

    /// <summary>Lấy danh sách công đoạn (kèm trình tự) đã cấu hình cho Line, sắp theo SequenceNumber.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageView)]
    public async Task<ActionResult<IReadOnlyList<LineStageSequenceDto>>> GetAll(int lineId, CancellationToken cancellationToken)
    {
        var items = await _lineStageSequenceService.GetByLineAsync(lineId, cancellationToken);
        return Ok(items);
    }

    /// <summary>Thêm 1 công đoạn từ danh mục master vào trình tự của Line (AC1).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageCreate)]
    public async Task<ActionResult<LineStageSequenceDto>> Add(int lineId, [FromBody] AddStageToLineRequest request, CancellationToken cancellationToken)
    {
        var created = await _lineStageSequenceService.AddAsync(lineId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { lineId }, created);
    }

    /// <summary>
    /// Gỡ 1 công đoạn khỏi trình tự của Line, tự động điều chỉnh lại trình tự còn lại (AC2) — chặn hẳn nếu đang
    /// có kế hoạch Running/Paused tại đúng công đoạn này.
    /// </summary>
    [HttpDelete("{stageId:int}")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageDelete)]
    public async Task<IActionResult> Remove(int lineId, int stageId, CancellationToken cancellationToken)
    {
        await _lineStageSequenceService.RemoveAsync(lineId, stageId, cancellationToken);
        return NoContent();
    }

    /// <summary>Sắp xếp lại toàn bộ trình tự công đoạn của Line — từ chối nếu trùng thứ tự (AC4) hoặc tạo vòng lặp (AC5) (AC3).</summary>
    [HttpPut("reorder")]
    [Authorize(Policy = PermissionPolicies.ProductionPlanStageUpdate)]
    public async Task<ActionResult<IReadOnlyList<LineStageSequenceDto>>> Reorder(int lineId, [FromBody] ReorderLineStageSequenceRequest request, CancellationToken cancellationToken)
    {
        var result = await _lineStageSequenceService.ReorderAsync(lineId, request, cancellationToken);
        return Ok(result);
    }
}
