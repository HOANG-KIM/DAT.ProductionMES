namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Request ghi nhận 1 lượt Scan NG (US-18 AC3/AC5, `POST api/v1/scans/ng`) — khác <see cref="CreateScanRequest"/>
/// (luồng OK bình thường) vì bắt buộc kèm <see cref="RejectionReason"/> (AC3 "bắt buộc nhập lý do trước khi ghi
/// nhận"), validate ở <c>CreateNgScanRequestValidator</c>.
/// </summary>
/// <remarks>
/// <see cref="WorkStationId"/> (thay đổi 18/08/2026, xem `Documents/BACKLOG-user-story.md` mục US-18 "Ghi chú
/// (18/08/2026, thay đổi yêu cầu)"): KHÁC <see cref="CreateScanRequest.WorkStationId"/> — endpoint này không
/// còn dùng scheme <c>StationApiKey</c> (đổi sang Bearer Tổ trưởng, ADR-005 mục 2, vì cần xác định danh tính
/// người xác nhận NG), nên KHÔNG còn claim trạm để đối chiếu. Giá trị này giờ là NGUỒN THẬT DUY NHẤT xác định
/// trạm (Station.Wpf tự khai đúng <c>StationOptions.WorkStationId</c> của chính nó — trạm không expose UI cho
/// việc gõ tay giá trị này, chấp nhận được theo đúng phân tích đã chốt). Người xác nhận (UserId/Username) lấy từ
/// claim Bearer token ở Controller, KHÔNG nằm trong DTO này (tránh giả mạo).
/// </remarks>
public class CreateNgScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }

    /// <summary>AC3/AC4: lý do lỗi dạng free text, bắt buộc nhập (không được để trống).</summary>
    public string RejectionReason { get; set; } = string.Empty;
}
