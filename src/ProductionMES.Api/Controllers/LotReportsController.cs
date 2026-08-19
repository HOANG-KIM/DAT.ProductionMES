using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.Reports;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Báo cáo Lot-centric (US-21/FR-21, vòng 3 — 18/08/2026, AC1-AC5): tìm/chọn 1 Lot rồi xem chi tiết vòng đời sản
/// xuất. Cùng permission <c>Report.View</c> với <see cref="ProductionReportsController"/> (bản Line-centric vòng
/// 1/2, giữ lại làm nhánh phụ theo AC6) — xác thực bằng scheme mặc định (Bearer/cookie, ADR-003) + permission
/// động (ADR-004), KHÔNG dùng <c>StationApiKey</c>.
/// </summary>
[ApiController]
[Route("api/v1/reports/lots")]
[Authorize(Policy = PermissionPolicies.ReportView)]
public class LotReportsController : ControllerBase
{
    /// <summary>US-23: Content-Type chuẩn cho file .xlsx theo Open Packaging Conventions (OOXML).</summary>
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ILotReportService _lotReportService;
    private readonly ILotReportExportService _lotReportExportService;

    public LotReportsController(ILotReportService lotReportService, ILotReportExportService lotReportExportService)
    {
        _lotReportService = lotReportService;
        _lotReportExportService = lotReportExportService;
    }

    /// <summary>AC1/AC2 — gợi ý (autocomplete) danh sách Lot khớp gần đúng <paramref name="search"/>. Trả mảng rỗng nếu không khớp Lot nào (AC2 "Không tìm thấy Lot" — không phải lỗi hệ thống).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LotSearchItemDto>>> Search([FromQuery] string? search, CancellationToken cancellationToken = default)
    {
        var result = await _lotReportService.SearchLotsAsync(search, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// AC3/AC4/AC5 — chi tiết 1 Lot: Model/Khách hàng/Revision (đồng nhất hay không), danh sách (Line, Công đoạn)
    /// kèm OK/NG, lọc theo khoảng thời gian tùy chọn.
    /// </summary>
    /// <param name="lot">Mã Lot cần xem chi tiết (so khớp tuyệt đối).</param>
    /// <param name="from">AC5 — cận dưới (bao gồm) của <c>ScannedAtUtc</c>, UTC ISO 8601. Không truyền = không giới hạn.</param>
    /// <param name="to">AC5 — cận trên (bao gồm) của <c>ScannedAtUtc</c>, UTC ISO 8601. Không truyền = không giới hạn.</param>
    [HttpGet("{lot}")]
    public async Task<ActionResult<LotSummaryDto>> GetSummary(
        string lot, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await _lotReportService.GetLotSummaryAsync(lot, from, to, cancellationToken);
        // AC2: "Không tìm thấy Lot" -> 404 (ProblemDetails chuẩn theo API-Conventions.md mục 5/6), KHÔNG phải lỗi hệ thống.
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// US-23 (FR-23, phạm vi thu hẹp 19/08/2026 — CHỈ báo cáo "Theo Lot") — xuất file Excel (.xlsx, 2 sheet: Tổng
    /// hợp + Chi tiết lượt scan) cho đúng 1 Lot, cùng bộ lọc <paramref name="from"/>/<paramref name="to"/> đang
    /// truyền cho <see cref="GetSummary"/>. Logic sinh file đặt ở <see cref="ILotReportExportService"/> (Application
    /// layer) — Controller chỉ gọi Service rồi trả file, không viết logic Excel trực tiếp ở đây.
    /// </summary>
    [HttpGet("{lot}/export")]
    public async Task<IActionResult> Export(
        string lot, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var content = await _lotReportExportService.ExportAsync(lot, from, to, cancellationToken);
        if (content is null)
        {
            // AC2: "Không tìm thấy Lot" -> 404, cùng quy tắc GetSummary.
            return NotFound();
        }

        var fileName = $"bao-cao-lot-{lot}.xlsx";
        return File(content, XlsxContentType, fileName);
    }
}
