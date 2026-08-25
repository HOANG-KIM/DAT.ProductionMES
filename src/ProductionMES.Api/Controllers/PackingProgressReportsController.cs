using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.Reports;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Theo dõi tiến độ đóng thùng ở mức quản lý (US-26/FR-26) — tổng hợp mọi kế hoạch đang Running tại công đoạn
/// "Đóng thùng" trên toàn nhà máy, đối chiếu % hoàn thành so với "Tổng số lượng Lot" (US-21a). Cùng permission
/// <c>Report.View</c> với <see cref="LotReportsController"/>/<see cref="ProductionReportsController"/> (cùng
/// nhóm "báo cáo cấp quản lý") — xác thực bằng scheme mặc định (Bearer/cookie, ADR-003) + permission động
/// (ADR-004), KHÔNG dùng <c>StationApiKey</c>.
/// </summary>
[ApiController]
[Route("api/v1/reports/packing-progress")]
[Authorize(Policy = PermissionPolicies.ReportView)]
public class PackingProgressReportsController : ControllerBase
{
    /// <summary>US-23 (tiền lệ) — Content-Type chuẩn cho file .xlsx theo Open Packaging Conventions (OOXML).</summary>
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IPackingProgressReportService _packingProgressReportService;
    private readonly IPackingProgressReportExportService _packingProgressReportExportService;

    public PackingProgressReportsController(
        IPackingProgressReportService packingProgressReportService,
        IPackingProgressReportExportService packingProgressReportExportService)
    {
        _packingProgressReportService = packingProgressReportService;
        _packingProgressReportExportService = packingProgressReportExportService;
    }

    /// <summary>
    /// AC1 (viết lại 25/08/2026) — gợi ý (autocomplete) các Lot đang <c>Running</c> tại công đoạn "Đóng thùng"
    /// khớp gần đúng <paramref name="q"/>, KHÔNG lọc theo Line. Trả mảng rỗng nếu <paramref name="q"/> trống hoặc
    /// không khớp Lot nào — không phải lỗi hệ thống.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<PackingProgressSearchItemDto>>> Search(
        [FromQuery] string? q, CancellationToken cancellationToken = default)
    {
        var result = await _packingProgressReportService.SearchAsync(q, cancellationToken);
        return Ok(result);
    }

    /// <summary>AC2/AC3/AC4 — (các) dòng kết quả chi tiết ứng với bộ lọc (thường chỉ truyền <c>lot</c> sau khi đã chọn ở AC1).</summary>
    [HttpGet]
    public async Task<ActionResult<PackingProgressReportDto>> Get(
        [FromQuery] int? lineId, [FromQuery] string? lot, [FromQuery] string? model, CancellationToken cancellationToken = default)
    {
        var query = new PackingProgressReportQuery { LineId = lineId, Lot = lot, Model = model };
        var result = await _packingProgressReportService.GetReportAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>AC6 — danh sách TẤT CẢ thùng (Completed lẫn InProgress) của 1 dòng báo cáo (Line + Lot).</summary>
    [HttpGet("boxes")]
    public async Task<ActionResult<IReadOnlyList<PackingProgressReportBoxDto>>> GetBoxes(
        [FromQuery] int lineId, [FromQuery] string lot, CancellationToken cancellationToken = default)
    {
        var result = await _packingProgressReportService.GetBoxesAsync(lineId, lot, cancellationToken);
        return Ok(result);
    }

    /// <summary>AC7/AC8 — danh sách lượt scan OK đã cộng vào 1 thùng cụ thể.</summary>
    [HttpGet("boxes/{boxId:int}/scans")]
    public async Task<ActionResult<PackingProgressReportBoxScansDto>> GetBoxScans(int boxId, CancellationToken cancellationToken = default)
    {
        var result = await _packingProgressReportService.GetBoxScansAsync(boxId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// AC9-AC13 — xuất file Excel (.xlsx, 3 sheet: "Tổng quan"/"Danh sách thùng"/"Lượt scan") cho ĐÚNG 1 dòng báo
    /// cáo (Line + Lot), hành động theo TỪNG DÒNG (KHÔNG phải nút xuất chung toàn bảng). Cùng permission
    /// <c>Report.View</c> (AC13 — không tạo permission riêng). Logic sinh file đặt ở
    /// <see cref="IPackingProgressReportExportService"/> (Application layer) — Controller chỉ gọi Service rồi trả
    /// file, không viết logic Excel trực tiếp ở đây.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] int lineId, [FromQuery] string lot, CancellationToken cancellationToken = default)
    {
        var content = await _packingProgressReportExportService.ExportAsync(lineId, lot, cancellationToken);
        if (content is null)
        {
            // Không còn dòng nào khớp Line + Lot tại thời điểm xuất (vd kế hoạch không còn Running) -> 404.
            return NotFound();
        }

        var fileName = $"bao-cao-lich-su-dong-thung-{SanitizeFileName(lot)}-{DateTime.Now:yyyyMMdd}.xlsx";
        return File(content, XlsxContentType, fileName);
    }

    /// <summary>AC9 — chuẩn hoá/loại bỏ ký tự không hợp lệ cho tên file (Lot có thể chứa ký tự đặc biệt), cùng cách xử lý đã dùng ở <c>PackingBoxService</c>/<c>PackingModelConfigService</c> (US-24/US-25).</summary>
    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
