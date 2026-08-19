namespace ProductionMES.Domain.Entities;

/// <summary>
/// 1 lần Tổ trưởng "Mở khóa rework" cho 1 tem tại 1 công đoạn (US-19, FR-19) — sau khi tem bị NG
/// (<see cref="Scan.Result"/> = <see cref="Enums.ScanResult.Ng"/>) tại đúng (<see cref="TagCode"/>, <see cref="StageId"/>).
/// </summary>
/// <remarks>
/// GHI MỖI LẦN mở khóa (không phải 1 cờ tĩnh cập nhật tại chỗ) — vì AC4 cho phép lặp lại không giới hạn số lần
/// NG/mở khóa tại cùng 1 công đoạn, và FR-19 yêu cầu log lại từng thao tác mở khóa (ai duyệt, thời điểm, ghi chú).
///
/// Trạng thái "tem đang bị khóa rework hay không" KHÔNG lưu ở đây (không có cột <c>IsLocked</c> tĩnh) — được SUY
/// RA (xem <c>ProductionMES.Application.Services.ReworkUnlocks.ReworkLockCalculator</c>) bằng cách so sánh bản
/// ghi <see cref="Scan"/> mới nhất và bản ghi <see cref="ReworkUnlock"/> mới nhất tại cùng (TagCode, StageId):
/// nếu bản ghi Scan mới nhất có <c>Result = Ng</c> VÀ không có ReworkUnlock nào diễn ra SAU thời điểm Ng đó ->
/// đang khóa. Cách suy luận động này tránh phải đồng bộ 2 nguồn dữ liệu (cờ tĩnh + lịch sử) mỗi khi ghi Scan mới.
///
/// KHÔNG có ràng buộc khoá ngoại ở DB (CLAUDE.md) — <see cref="StageId"/> là cột tham chiếu thuần tới
/// <c>Stage.Id</c>, toàn vẹn xử lý ở tầng Application.
/// </remarks>
public class ReworkUnlock
{
    public int Id { get; set; }

    /// <summary>Mã tem được mở khóa rework.</summary>
    public string TagCode { get; set; } = string.Empty;

    /// <summary>Công đoạn (Stage master) mà tem đang bị khóa — cùng field dùng để chống trùng tem/khóa rework toàn hệ thống, không phân biệt Line.</summary>
    public int StageId { get; set; }

    /// <summary>Id tài khoản Tổ trưởng/Admin đã thực hiện thao tác mở khóa (AC2/AC6 — permission <c>Scan.ReworkUnlock</c>).</summary>
    public int UnlockedByUserId { get; set; }

    /// <summary>Tên đăng nhập của <see cref="UnlockedByUserId"/> tại thời điểm mở khóa (snapshot, không tra cứu động).</summary>
    public string UnlockedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm mở khóa — lưu ĐÚNG giờ local hệ thống, KHÔNG quy đổi UTC (đổi ý 19/08/2026, xem
    /// API-Conventions.md mục 10) — mốc dùng để so sánh với <see cref="Scan.ScannedAtUtc"/> của lần Ng gần nhất
    /// khi suy luận trạng thái khóa (cùng đơn vị giờ local, so sánh trực tiếp được).
    /// </summary>
    public DateTime UnlockedAtUtc { get; set; }

    /// <summary>Ghi chú của Tổ trưởng khi mở khóa (AC2 "ghi chú nếu có") — tùy chọn, có thể để trống.</summary>
    public string? Note { get; set; }
}
