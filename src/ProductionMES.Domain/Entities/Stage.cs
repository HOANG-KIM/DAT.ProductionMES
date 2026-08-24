namespace ProductionMES.Domain.Entities;

/// <summary>
/// Công đoạn (danh mục master dùng chung toàn hệ thống, vd. Lắp ráp, Thông điện, Ngoại quan) (FR-02/US-02).
/// Không gắn cố định với 1 Line — cùng 1 công đoạn có thể được áp dụng ở nhiều Line khác nhau (AC2), phục vụ
/// đúng rule chống trùng tem toàn hệ thống ở FR-08 (chống trùng tem xét theo (Mã tem, Công đoạn), không theo Line).
/// </summary>
public class Stage
{
    public int Id { get; set; }

    /// <summary>Tên công đoạn, bắt buộc nhập (AC1).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả công đoạn, không bắt buộc.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động. Mặc định <c>true</c> khi tạo mới.
    /// Vô hiệu hóa (AC3) chỉ đổi cờ này thành <c>false</c> (soft-delete) — không xóa cứng, giữ nguyên dữ liệu
    /// lịch sử/kế hoạch đã gắn với công đoạn.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// US-25/FR-25 (bổ sung 24/08/2026): đánh dấu đây là công đoạn "Đóng thùng" đặc thù (đếm số lượng theo Quy
    /// cách đóng gói — US-24, tự động in tem thùng, chống trùng tem yêu cầu Supervisor xác nhận-đã-biết thay vì
    /// chặn cứng như mặc định). Mặc định <c>false</c> — mọi Stage khác giữ nguyên luồng scan tiêu chuẩn (AC14).
    /// KHÔNG suy luận từ <see cref="Name"/> (free-text, dễ gõ sai/đổi tên) — Admin CHỦ ĐỘNG đánh dấu đúng 1 Stage
    /// làm công đoạn Đóng thùng khi khai báo danh mục (US-02), quyết định thiết kế của US-25 (không có trong SRS
    /// gốc, ghi chú lại theo yêu cầu CLAUDE.md). AC1 vẫn đúng: "Đóng thùng" vẫn là 1 Stage bình thường trong danh
    /// mục/trình tự — cờ này chỉ là METADATA bổ sung, không tạo luồng/API riêng ngoài quy tắc chung FR-08.
    /// </summary>
    public bool IsPackingStage { get; set; }
}
