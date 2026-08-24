using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.PackingModelConfigs;
using ProductionMES.Application.Services.PackingModelConfigs;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Cấu hình Quy cách đóng gói theo Model (US-24/FR-24) — CRUD + upload/download file mẫu tem in (template
/// .xlsx). Dùng chung 1 nguồn duy nhất cho cả web-admin (Admin) và Station.Wpf (Tổ trưởng nâng quyền tại trạm,
/// AC6) — cả 2 client gọi đúng cùng bộ endpoint này, xác thực bằng scheme mặc định (Bearer/cookie) + permission
/// động (ADR-004).
/// </summary>
[ApiController]
[Route("api/v1/packing-model-configs")]
public class PackingModelConfigsController : ControllerBase
{
    /// <summary>Content-Type chuẩn cho file .xlsx (Open Packaging Conventions/OOXML) — cùng hằng số dùng ở <c>LotReportsController</c> (US-23).</summary>
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Giới hạn kích thước file mẫu tem tải lên — 5MB đủ rộng cho 1 file Excel mẫu tem đơn giản, chặn upload bất thường.</summary>
    private const long MaxTemplateFileSizeBytes = 5 * 1024 * 1024;

    private readonly IPackingModelConfigService _service;

    public PackingModelConfigsController(IPackingModelConfigService service)
    {
        _service = service;
    }

    /// <summary>Toàn bộ cấu hình (AC3) — danh mục nhỏ, không phân trang (cùng quy ước Line/Stage hiện có).</summary>
    [HttpGet]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigView)]
    public async Task<ActionResult<IReadOnlyList<PackingModelConfigDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _service.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    /// <summary>Lấy chi tiết 1 cấu hình theo Id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigView)]
    public async Task<ActionResult<PackingModelConfigDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// AC9 — tra cứu cấu hình theo Model, không phân biệt hoa/thường + tự trim khoảng trắng. Trả 404 nếu Model
    /// chưa từng được cấu hình (dùng ở US-25 sau này để kiểm tra AC11/AC8, và ở UI để hiển thị "đã có cấu hình chưa").
    /// </summary>
    [HttpGet("lookup")]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigView)]
    public async Task<ActionResult<PackingModelConfigDto>> GetByModel([FromQuery] string model, CancellationToken cancellationToken)
    {
        var item = await _service.GetByModelAsync(model, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>AC9 — gợi ý autocomplete Model đã có cấu hình, khớp gần đúng <paramref name="search"/>. Trả mảng rỗng nếu không khớp Model nào.</summary>
    [HttpGet("suggest-models")]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigView)]
    public async Task<ActionResult<IReadOnlyList<string>>> SuggestModels([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var items = await _service.SuggestModelsAsync(search, cancellationToken);
        return Ok(items);
    }

    /// <summary>Tạo mới cấu hình cho 1 Model (AC1).</summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigCreate)]
    public async Task<ActionResult<PackingModelConfigDto>> Create([FromBody] CreatePackingModelConfigRequest request, CancellationToken cancellationToken)
    {
        var updatedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        var created = await _service.CreateAsync(request, updatedByUserName, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Sửa cấu hình đã có (AC2) — không đổi Model.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigUpdate)]
    public async Task<ActionResult<PackingModelConfigDto>> Update(int id, [FromBody] UpdatePackingModelConfigRequest request, CancellationToken cancellationToken)
    {
        var updatedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        var updated = await _service.UpdateAsync(id, request, updatedByUserName, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// AC4 — tải lên (thay thế) file mẫu tem in cho 1 cấu hình. Endpoint <c>multipart/form-data</c> ĐẦU TIÊN của
    /// dự án (chưa có tiền lệ) — dùng <see cref="IFormFile"/> chuẩn ASP.NET Core, giới hạn kích thước qua
    /// <see cref="RequestSizeLimitAttribute"/>, chỉ nhận đúng phần mở rộng .xlsx (kiểm tra ở
    /// <see cref="IPackingModelConfigService.UploadTemplateAsync"/> — ném <see cref="BusinessRuleException"/> ->
    /// 409 nếu sai định dạng/file rỗng, đúng quy ước lỗi nghiệp vụ hiện có thay vì tự chế 1 dạng lỗi mới).
    /// </summary>
    [HttpPost("{id:int}/template")]
    [RequestSizeLimit(MaxTemplateFileSizeBytes)]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigUpdate)]
    public async Task<ActionResult<PackingModelConfigDto>> UploadTemplate(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessRuleException("Vui lòng chọn file mẫu tem (.xlsx) để tải lên.");
        }

        var updatedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        await using var stream = file.OpenReadStream();
        var updated = await _service.UploadTemplateAsync(id, stream, file.FileName, updatedByUserName, cancellationToken);
        return Ok(updated);
    }

    /// <summary>AC5 — tải xuống file mẫu tem đang cấu hình. Trả 404 nếu Model chưa từng có ai tải mẫu tem lên (xử lý bằng <see cref="EntityNotFoundException"/> qua <see cref="ProductionMES.Api.ExceptionHandling.GlobalExceptionHandler"/>).</summary>
    [HttpGet("{id:int}/template")]
    [Authorize(Policy = PermissionPolicies.PackingModelConfigView)]
    public async Task<IActionResult> DownloadTemplate(int id, CancellationToken cancellationToken)
    {
        var (content, fileName) = await _service.DownloadTemplateAsync(id, cancellationToken);
        return File(content, XlsxContentType, fileName);
    }
}
