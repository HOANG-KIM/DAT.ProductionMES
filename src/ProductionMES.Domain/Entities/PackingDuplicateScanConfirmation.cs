namespace ProductionMES.Domain.Entities;

/// <summary>
/// 1 lần Supervisor xác nhận "đã biết tình huống" cho 1 lượt quét tem TRÙNG tại công đoạn "Đóng thùng" (US-25 AC8,
/// mục 6 quy tắc 16 SRS) — GHI MỖI LẦN xác nhận (cùng idiom <see cref="ReworkUnlock"/>, US-19), không phải 1 cờ
/// tĩnh cập nhật tại chỗ, vì 1 tem có thể bị quét trùng nhiều lần khác nhau và mỗi lần cần xác nhận riêng.
/// </summary>
/// <remarks>
/// Đây CHỈ là bản ghi AUDIT (ai đã xử lý, khi nào, ghi chú gì) — KHÔNG làm thay đổi kết quả của lượt scan trùng đã
/// bị từ chối (<see cref="Scan.Result"/> = <see cref="Enums.ScanResult.DuplicateTag"/> vẫn giữ nguyên, được tạo
/// bởi luồng scan tiêu chuẩn FR-08 y hệt mọi công đoạn khác — xem remarks <see cref="Scan"/>). KHÔNG cộng thêm số
/// lượng vào <see cref="PackingBox.ScannedQuantity"/>, KHÔNG tạo thêm bản ghi <see cref="Scan"/> nào (AC8 "Đây
/// KHÔNG phải ngoại lệ ghi đè của FR-08").
///
/// KHÔNG có ràng buộc khoá ngoại ở DB (CLAUDE.md) — <see cref="StageId"/>/<see cref="ScanId"/> là cột tham chiếu
/// thuần, toàn vẹn xử lý ở tầng Application.
/// </remarks>
public class PackingDuplicateScanConfirmation
{
    public int Id { get; set; }

    /// <summary>Mã tem bị quét trùng đã được xác nhận.</summary>
    public string TagCode { get; set; } = string.Empty;

    /// <summary>Công đoạn "Đóng thùng" (Stage master) nơi xảy ra trùng tem.</summary>
    public int StageId { get; set; }

    /// <summary>Id bản ghi <see cref="Scan"/> (Result = DuplicateTag) gần nhất tại (TagCode, StageId) được xác nhận — tham chiếu thuần, phục vụ truy vết/tra cứu lịch sử (AC10).</summary>
    public int ScanId { get; set; }

    /// <summary>Id tài khoản Supervisor/Admin đã xác nhận (AC8 — tái sử dụng cơ chế re-auth Supervisor của US-18).</summary>
    public int ConfirmedByUserId { get; set; }

    /// <summary>Tên đăng nhập của <see cref="ConfirmedByUserId"/> tại thời điểm xác nhận (snapshot, không tra cứu động).</summary>
    public string ConfirmedByUserName { get; set; } = string.Empty;

    /// <summary>Thời điểm xác nhận — giờ local hệ thống (cùng quy ước <see cref="ReworkUnlock.UnlockedAtUtc"/>).</summary>
    public DateTime ConfirmedAtUtc { get; set; }

    /// <summary>Ghi chú tùy chọn của Supervisor khi xác nhận.</summary>
    public string? Note { get; set; }
}
