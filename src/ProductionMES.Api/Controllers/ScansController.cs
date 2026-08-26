using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authentication;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.Scans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Ghi nhận lượt scan tem tại trạm (US-07/US-08, FR-07/FR-08). Xác thực bằng scheme riêng "StationApiKey"
/// (ADR-005) — KHÔNG đi qua hệ permission Resource.Action động (ADR-004), vì không có User nào gắn với request
/// dùng scheme này.
/// </summary>
/// <remarks>
/// US-18 (thay đổi yêu cầu 18/08/2026): action tạo Scan NG (<c>POST api/v1/scans/ng</c>) đã CHUYỂN sang
/// <c>ScanNgController</c> riêng — bấm nút "NG" nay bắt buộc đăng nhập Tổ trưởng (Bearer token, ADR-005 mục 2),
/// khác hẳn Operator không đăng nhập cá nhân của scheme "StationApiKey" ở Controller này. Không thể gộp chung
/// scheme ở đúng 1 action (class này cần "trạm" qua StationApiKey, action NG cần "người" qua Bearer + permission
/// <c>Scan.ConfirmNg</c>) — xem lý do tách Controller trong `Documents/BACKLOG-user-story.md` mục US-18.
/// </remarks>
[ApiController]
[Route("api/v1/scans")]
[Authorize(AuthenticationSchemes = StationApiKeyDefaults.AuthenticationScheme)]
public class ScansController : ControllerBase
{
    private readonly IScanService _scanService;

    public ScansController(IScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// Ghi nhận 1 lượt scan. Luôn trả 201 kèm kết quả (kể cả khi bị từ chối — DuplicateTag/PreviousStageNotPassed/
    /// WaitingReworkUnlock là kết quả nghiệp vụ hợp lệ theo FR-08, không phải lỗi HTTP). US-27 (25/08/2026): CHỈ
    /// bản ghi Scan Result=Ok mới thực sự được lưu ở đây — các kết quả bị từ chối tự động KHÔNG còn được lưu ngay
    /// (đảo ngược FR-10 cũ), client hiển thị banner Lưu/Thoát rồi gọi <c>POST api/v1/scans/reject-confirmations</c>
    /// (<see cref="ScanRejectConfirmationsController"/>) nếu Tổ trưởng xác nhận cần lưu (AC3/AC5/AC6).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ScanResultDto>> Create([FromBody] CreateScanRequest request, CancellationToken cancellationToken)
    {
        // ADR-005: WorkStationId THẬT lấy từ danh tính trạm đã xác thực (claim), không tin trực tiếp
        // request.WorkStationId dù StationApiKeyAuthenticationHandler đã đối chiếu ở tầng auth (AC6).
        var workStationIdClaim = User.FindFirst(StationApiKeyDefaults.WorkStationIdClaimType)?.Value;
        if (!int.TryParse(workStationIdClaim, out var workStationId))
        {
            return Unauthorized();
        }

        var result = await _scanService.CreateAsync(workStationId, request.TagCode, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// US-18 AC4: danh sách lý do lỗi đã từng nhập cho đúng công đoạn <paramref name="stageId"/>, dùng làm gợi ý
    /// autocomplete khi nhập lý do Scan NG mới — không bắt buộc chọn từ danh sách này (vẫn free text).
    /// </summary>
    [HttpGet("ng-reasons")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetNgReasonSuggestions([FromQuery] int stageId, CancellationToken cancellationToken)
    {
        var suggestions = await _scanService.GetNgReasonSuggestionsAsync(stageId, cancellationToken);
        return Ok(suggestions);
    }
}
