namespace ProductionMES.Domain.Entities;

/// <summary>
/// Line sản xuất (dây chuyền vật lý) — danh mục nền tảng cho cấu hình kế hoạch, trạm, luồng scan (FR-01/US-01).
/// </summary>
/// <remarks>
/// <see cref="Id"/> dùng kiểu int identity (không phải Guid): Line là dữ liệu danh mục do Admin tạo qua giao diện
/// cấu hình (online, có kết nối server), số lượng nhỏ (vài chục bản ghi theo khảo sát thực tế nhà máy — xem mục 8.2
/// SRS), không cần sinh Id offline tại client như dữ liệu lượt scan (US-16) nên không cần Guid để chống trùng khi
/// đồng bộ. Auto-increment cũng cho Id ngắn gọn, dễ đọc khi thao tác trực tiếp trên các màn hình cấu hình.
/// </remarks>
public class Line
{
    public int Id { get; set; }

    /// <summary>Tên Line, bắt buộc nhập (AC1).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả Line, không bắt buộc.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động của Line. Mặc định <c>true</c> khi tạo mới (AC1).
    /// Vô hiệu hóa Line (AC3) chỉ đổi cờ này thành <c>false</c> (soft-delete) — không xóa cứng bản ghi,
    /// để giữ nguyên dữ liệu lịch sử scan/kế hoạch đã gắn với Line.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
