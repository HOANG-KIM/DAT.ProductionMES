using ProductionMES.Application.DTOs.Common;
using ProductionMES.Application.DTOs.Scans;

namespace ProductionMES.Application.Services.Scans;

public interface IScanService
{
    /// <summary>
    /// Ghi nhận 1 lượt scan tem tại trạm (US-07/US-08, FR-07/FR-08). Luôn lưu lại bản ghi Scan bất kể kết quả
    /// (FR-10) — kể cả khi bị từ chối do trùng tem/chưa qua công đoạn liền trước.
    /// </summary>
    /// <param name="workStationId">
    /// Id trạm THẬT lấy từ danh tính đã xác thực (claim), không phải từ request body — nguồn thật duy nhất
    /// dùng để xác định Line/Stage của lượt scan (ADR-005).
    /// </param>
    /// <param name="tagCode">Mã tem được scan.</param>
    Task<ScanResultDto> CreateAsync(int workStationId, string tagCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi nhận 1 lượt Scan NG (US-18 AC3/AC5, cập nhật 18/08/2026 — thay đổi yêu cầu bổ sung đăng nhập Tổ
    /// trưởng bắt buộc) — luôn ghi <c>Result = ScanResult.Ng</c> kèm <paramref name="rejectionReason"/>, KHÔNG
    /// chạy 2 bước kiểm tra chống trùng/công đoạn liền trước của <see cref="CreateAsync"/> (tem lỗi được phép
    /// quét bất kể trạng thái trước đó — xem remarks <see cref="ScanService"/>). Tem bị khóa ở công đoạn kế tiếp
    /// là hệ quả tự nhiên của việc KHÔNG có bản ghi <c>Ok</c> tại công đoạn này, không cần xử lý riêng.
    /// </summary>
    /// <param name="workStationId">
    /// Id trạm — KHÁC <see cref="CreateAsync"/>: endpoint Scan NG (`POST api/v1/scans/ng`) không còn dùng scheme
    /// StationApiKey (đổi sang Bearer Tổ trưởng, ADR-005 mục 2), nên giá trị này lấy từ request body do
    /// Station.Wpf tự khai (trạm nào gửi request thì trạm đó chịu trách nhiệm khai đúng, xem
    /// `Documents/BACKLOG-user-story.md` mục US-18).
    /// </param>
    /// <param name="tagCode">Mã tem sản phẩm lỗi.</param>
    /// <param name="rejectionReason">Lý do lỗi (free text, bắt buộc — validate ở FluentValidation lẫn Service).</param>
    /// <param name="confirmedByUserId">
    /// Id tài khoản Tổ trưởng/Admin/Manager đã đăng nhập lại (re-auth, AC2a) ngay trước khi kích hoạt Chế độ
    /// Scan NG — LẤY TỪ CLAIM token Bearer đã xác thực ở Controller (<c>ClaimTypes.NameIdentifier</c>), KHÔNG
    /// nhận trực tiếp từ DTO client gửi lên, để tránh giả mạo.
    /// </param>
    /// <param name="confirmedByUserName">Tên đăng nhập của <paramref name="confirmedByUserId"/> — cũng lấy từ claim, cùng lý do trên.</param>
    Task<ScanResultDto> CreateNgAsync(
        int workStationId, string tagCode, string rejectionReason, int confirmedByUserId, string confirmedByUserName, CancellationToken cancellationToken = default);

    /// <summary>
    /// US-18 AC4: danh sách lý do lỗi đã từng nhập cho đúng công đoạn <paramref name="stageId"/> (phục vụ gợi ý
    /// autocomplete khi nhập lý do lỗi mới) — không trùng lặp, sắp theo lần dùng gần nhất trước (lý do mới nhập
    /// gần đây nhiều khả năng liên quan tới vấn đề chất lượng đang xảy ra hơn lý do cũ).
    /// </summary>
    Task<IReadOnlyList<string>> GetNgReasonSuggestionsAsync(int stageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tra cứu lịch sử scan (US-10 AC2/AC3) — theo mã tem, hoặc theo trạm/Line/khoảng thời gian, có thể kết hợp
    /// nhiều điều kiện cùng lúc (AND). Kết quả luôn sắp theo <c>ScannedAtUtc</c> tăng dần (AC2 "sắp xếp theo thời
    /// gian") và phân trang theo `Documents/API-Conventions.md` mục 9.
    /// </summary>
    Task<PagedResult<ScanHistoryItemDto>> GetHistoryAsync(ScanHistoryQuery query, CancellationToken cancellationToken = default);
}
