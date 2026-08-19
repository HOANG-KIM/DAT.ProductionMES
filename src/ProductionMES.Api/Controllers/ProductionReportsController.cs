using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.Reports;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Báo cáo tổng hợp ACTUAL/PLAN/BALANCE theo Line/công đoạn (US-21, FR-21). Dành cho Admin/Tổ trưởng/Ban quản lý
/// đăng nhập cá nhân — xác thực bằng scheme mặc định (Bearer/cookie, ADR-003) + permission động (ADR-004), KHÔNG
/// dùng <c>StationApiKey</c> — cùng nguyên tắc với <see cref="ScanHistoryController"/> (US-10).
/// </summary>
[ApiController]
[Route("api/v1/reports/production-summary")]
public class ProductionReportsController : ControllerBase
{
    private readonly IProductionReportService _productionReportService;

    public ProductionReportsController(IProductionReportService productionReportService)
    {
        _productionReportService = productionReportService;
    }

    /// <summary>
    /// Lấy báo cáo tổng hợp cho mọi cặp (Line, Công đoạn) đang có trạm làm việc hoạt động, hoặc chỉ 1 Line nếu
    /// <paramref name="lineId"/> được truyền.
    /// </summary>
    /// <param name="lineId">Lọc theo đúng 1 Line — tùy chọn.</param>
    /// <param name="stageId">AC6 (mới) — lọc theo đúng 1 công đoạn — tùy chọn.</param>
    /// <param name="lot">AC6 (mới) — lọc theo đúng 1 Lot — tùy chọn.</param>
    /// <param name="model">AC6 (mới) — lọc theo Model sản phẩm — tùy chọn.</param>
    /// <param name="customer">AC6 (mới) — lọc theo Khách hàng — tùy chọn.</param>
    /// <param name="revision">AC6 (mới) — lọc theo Revision — tùy chọn.</param>
    /// <param name="from">
    /// AC2 — cận dưới (bao gồm) khoảng thời gian xem báo cáo, UTC ISO 8601. Không truyền cùng <paramref name="to"/>
    /// → AC1 "xem theo thời gian thực" (ACTUAL/PLAN/BALANCE lũy kế từ kế hoạch đang chạy tới hiện tại).
    /// </param>
    /// <param name="to">AC2 — cận trên (bao gồm) khoảng thời gian xem báo cáo, UTC ISO 8601.</param>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.ReportView)]
    public async Task<ActionResult<ProductionReportDto>> Get(
        [FromQuery] int? lineId,
        [FromQuery] int? stageId,
        [FromQuery] string? lot,
        [FromQuery] string? model,
        [FromQuery] string? customer,
        [FromQuery] string? revision,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = new ProductionReportQuery
        {
            LineId = lineId,
            StageId = stageId,
            Lot = lot,
            Model = model,
            Customer = customer,
            Revision = revision,
            FromUtc = from,
            ToUtc = to,
        };
        var result = await _productionReportService.GetReportAsync(query, cancellationToken);
        return Ok(result);
    }
}
