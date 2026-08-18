using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Api.Authorization;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.Scans;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Ghi nhận Scan NG tại trạm (US-18 AC3/AC5, thay đổi yêu cầu 18/08/2026) — TÁCH RIÊNG khỏi
/// <see cref="ScansController"/> vì đổi hẳn cơ chế xác thực: <see cref="ScansController"/> dùng scheme
/// "StationApiKey" (ADR-005, "trạm" là đơn vị xác thực, không có User nào gắn với request), còn action này BẮT
/// BUỘC đăng nhập Tổ trưởng (re-auth mỗi lần, AC2a) NGAY trước khi vào Chế độ Scan NG — dùng scheme Bearer mặc
/// định (giống <c>ProductionPlanStagesController</c>, ADR-005 mục 2) + permission động <c>Scan.ConfirmNg</c>
/// (ADR-004, seed cho Supervisor/Admin/Manager).
/// </summary>
/// <remarks>
/// Không thể xác định CẢ 2 danh tính (trạm + người xác nhận) cùng lúc trong 1 request theo đúng 1 scheme — quyết
/// định kỹ thuật (xem `Documents/BACKLOG-user-story.md` mục US-18): giữ Bearer làm nguồn xác thực DUY NHẤT
/// (bắt buộc, dùng để lấy <c>ConfirmedByUserId</c>/<c>ConfirmedByUserName</c> từ claim — KHÔNG tin theo DTO client
/// gửi lên, tránh giả mạo), còn <see cref="CreateNgScanRequest.WorkStationId"/> nhận trực tiếp từ request body do
/// Station.Wpf tự khai (trạm biết đúng <c>StationOptions.WorkStationId</c> của chính nó, không expose UI cho việc
/// gõ tay giá trị này qua tay người dùng — rủi ro giả mạo chấp nhận được, khác hẳn API Key/mật khẩu Tổ trưởng).
/// </remarks>
[ApiController]
[Route("api/v1/scans/ng")]
public class ScanNgController : ControllerBase
{
    private readonly IScanService _scanService;

    public ScanNgController(IScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// US-18 AC3/AC5: ghi nhận 1 lượt Scan NG (Chế độ Scan NG tại trạm) kèm lý do lỗi bắt buộc. Luôn trả 201 —
    /// bản ghi Scan luôn được tạo mới (FR-10), không có nhánh "thành công nhưng không lưu" ở endpoint này.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PermissionPolicies.ScanConfirmNg)]
    public async Task<ActionResult<ScanResultDto>> Create([FromBody] CreateNgScanRequest request, CancellationToken cancellationToken)
    {
        // AC5/ADR-005: người xác nhận THẬT lấy từ claim JWT đã xác thực (chuẩn ClaimTypes.NameIdentifier/Name do
        // JwtTokenGenerator phát hành — cùng claim UserId/Username đang dùng cho web-admin/Station.Wpf Supervisor),
        // KHÔNG nhận trực tiếp từ request body để tránh giả mạo.
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var confirmedByUserId))
        {
            return Unauthorized();
        }

        // ConfirmedByUserName lưu Username (không phải FullName): JWT hiện chỉ mang claim Username
        // (ClaimTypes.Name) — thêm claim FullName mới sẽ buộc Controller reference thẳng
        // ProductionMES.Infrastructure (JwtTokenGenerator), vi phạm luật layer (CLAUDE.md). Username đã đủ để
        // truy vết đúng tài khoản xác nhận.
        var confirmedByUserName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(confirmedByUserName))
        {
            return Unauthorized();
        }

        var result = await _scanService.CreateNgAsync(
            request.WorkStationId, request.TagCode, request.RejectionReason, confirmedByUserId, confirmedByUserName, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
