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
}
