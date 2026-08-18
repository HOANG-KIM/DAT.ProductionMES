using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// 1 lượt scan tem tại trạm (FR-07/FR-08/FR-10, US-07/US-08). Ghi nhận mọi lượt scan, kể cả bị từ chối
/// (<see cref="Result"/> != <see cref="ScanResult.Ok"/>) — không có khái niệm "return lỗi mà không lưu DB".
/// </summary>
/// <remarks>
/// Luồng scan thường (US-07/US-08, <see cref="ScanResult.Ok"/>/<see cref="ScanResult.DuplicateTag"/>/
/// <see cref="ScanResult.PreviousStageNotPassed"/>) KHÔNG gắn với người dùng cụ thể (Operator không đăng nhập
/// cá nhân — ADR-005) — <see cref="ConfirmedByUserId"/>/<see cref="ConfirmedByUserName"/> luôn <c>null</c> cho
/// các bản ghi này.
///
/// US-18 (thay đổi yêu cầu 18/08/2026, xem `Documents/BACKLOG-user-story.md` mục US-18 "Ghi chú (18/08/2026,
/// thay đổi yêu cầu)"): bấm nút/quét mã "NG" nay BẮT BUỘC đăng nhập Tổ trưởng (re-auth mỗi lần) TRƯỚC khi vào
/// Chế độ Scan NG — <see cref="ConfirmedByUserId"/>/<see cref="ConfirmedByUserName"/> lưu đúng tài khoản đã
/// đăng nhập đó khi <see cref="Result"/> = <see cref="ScanResult.Ng"/>. 2 field NULLABLE vì: (a) không backfill
/// dữ liệu Ng cũ đã ghi trước thay đổi này (chấp nhận thiếu thông tin người xác nhận cho các bản ghi trước
/// 18/08/2026), (b) giữ đúng bất biến "chỉ Ng mới có người xác nhận" ở trên.
///
/// KHÔNG có ràng buộc UNIQUE(TagCode, StageId) ở DB: 1 tem có thể có nhiều bản ghi bị từ chối tại cùng
/// (TagCode, StageId), miễn tối đa 1 bản ghi có Result = Ok — ràng buộc "tối đa 1 Ok" xử lý ở ScanService
/// (business rule), không dựa vào DB constraint (US-18/US-19 rework sau này sẽ dùng lại đúng bảng này).
///
/// Chống trùng tem (FR-08 bước 1) và kiểm tra công đoạn liền trước (FR-08 bước 2) đều tra cứu theo
/// <see cref="StageId"/> (công đoạn master, dùng chung nhiều Line) trên TOÀN HỆ THỐNG, không giới hạn theo
/// <see cref="LineId"/>/<see cref="ProductionPlanId"/> — 2 field đó chỉ dùng để ghi nhận ngữ cảnh lượt scan
/// (phục vụ tra cứu lịch sử US-10), không dùng trong điều kiện chống trùng/kiểm tra trình tự.
///
/// US-10 (FR-05/FR-10, mục 6 quy tắc 14): các field <see cref="Customer"/>/<see cref="Model"/>/<see cref="Lot"/>/
/// <see cref="Revision"/>/<see cref="PlannedQuantity"/>/<see cref="TaktTimeSeconds"/> là SNAPSHOT bất biến sao
/// chép từ <see cref="ProductionPlan"/> tại đúng thời điểm scan — không phải tra cứu động qua
/// <see cref="ProductionPlanId"/>. Nếu sau này ai sửa các field tương ứng ở ProductionPlan, lịch sử scan cũ vẫn
/// giữ nguyên đúng giá trị đã ghi nhận lúc scan, tránh sai lệch traceability.
/// </remarks>
public class Scan
{
    public int Id { get; set; }

    /// <summary>Mã tem sản phẩm được scan.</summary>
    public string TagCode { get; set; } = string.Empty;

    /// <summary>Công đoạn master (Stage) mà lượt scan này thực hiện — dùng để chống trùng tem toàn hệ thống (FR-08).</summary>
    public int StageId { get; set; }

    /// <summary>Line mà trạm scan thuộc về tại thời điểm scan (ngữ cảnh, không dùng trong rule chống trùng/trình tự).</summary>
    public int LineId { get; set; }

    /// <summary>Trạm làm việc thực hiện lượt scan.</summary>
    public int WorkStationId { get; set; }

    /// <summary>Kế hoạch sản xuất đang active của Line tại thời điểm scan.</summary>
    public int ProductionPlanId { get; set; }

    /// <summary>Snapshot ProductionPlan.Customer tại thời điểm scan (US-10, xem remarks).</summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Model tại thời điểm scan (US-10, xem remarks).</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Lot tại thời điểm scan (US-10, xem remarks).</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Revision tại thời điểm scan (US-10, xem remarks) — có thể để trống.</summary>
    public string? Revision { get; set; }

    /// <summary>Snapshot ProductionPlan.PlannedQuantity tại thời điểm scan (US-10, xem remarks).</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>Snapshot ProductionPlan.TaktTimeSeconds tại thời điểm scan (US-10, xem remarks).</summary>
    public decimal TaktTimeSeconds { get; set; }

    /// <summary>Thời điểm scan (UTC).</summary>
    public DateTime ScannedAtUtc { get; set; }

    /// <summary>Kết quả lượt scan (FR-08/FR-10).</summary>
    public ScanResult Result { get; set; }

    /// <summary>
    /// Lý do bị từ chối, diễn giải rõ ràng cho người vận hành (vd "Chưa qua công đoạn: Lắp ráp" — AC-03 SRS
    /// mục 7). <c>null</c> khi <see cref="Result"/> = <see cref="ScanResult.Ok"/>.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// US-18 AC5 (thay đổi 18/08/2026): Id tài khoản Tổ trưởng/Admin/Manager đã đăng nhập lại (re-auth) ngay
    /// trước khi kích hoạt Chế độ Scan NG (AC2a), dùng cho đúng lượt Scan NG này. <c>null</c> cho mọi
    /// <see cref="Result"/> khác <see cref="ScanResult.Ng"/>, và cho các bản ghi Ng ghi trước 18/08/2026 (không
    /// backfill — xem remarks).
    /// </summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>Tên đăng nhập hiển thị của <see cref="ConfirmedByUserId"/> tại thời điểm scan (snapshot, không tra cứu động) — cùng điều kiện null như trên.</summary>
    public string? ConfirmedByUserName { get; set; }
}
