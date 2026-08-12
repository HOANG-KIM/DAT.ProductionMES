namespace ProductionMES.Domain.Entities;

/// <summary>
/// Trạm làm việc — vị trí vật lý (PC + màn hình + máy scan, có thể kèm Arduino) thực hiện đúng 1 công đoạn
/// của đúng 1 Line cụ thể (FR-04/US-04).
/// </summary>
public class WorkStation
{
    public int Id { get; set; }

    /// <summary>Tên trạm, bắt buộc nhập (AC1).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Line mà trạm này thuộc về — mỗi trạm gắn cố định đúng 1 Line (AC1).</summary>
    public int LineId { get; set; }

    /// <summary>Công đoạn mà trạm này thực hiện — mỗi trạm gắn cố định đúng 1 công đoạn (AC1).</summary>
    public int StageId { get; set; }

    /// <summary>
    /// Cờ bật/tắt sử dụng Arduino tại trạm (FR-11/US-11). Khi <c>true</c>, bắt buộc phải có đủ thông tin
    /// cổng COM (AC2); khi <c>false</c>, không yêu cầu (AC3).
    /// </summary>
    public bool UseArduino { get; set; }

    /// <summary>Tên cổng COM (vd "COM3") — bắt buộc khi <see cref="UseArduino"/> = true (AC2).</summary>
    public string? ComPort { get; set; }

    /// <summary>Baud rate kết nối Serial — bắt buộc khi <see cref="UseArduino"/> = true (AC2).</summary>
    public int? BaudRate { get; set; }

    /// <summary>Giao thức lệnh trao đổi với Arduino (vd "OK\n") — bắt buộc khi <see cref="UseArduino"/> = true (AC2).</summary>
    public string? CommandProtocol { get; set; }

    /// <summary>
    /// Trạng thái hoạt động của trạm. Mặc định <c>true</c> khi tạo mới — theo cùng pattern soft-delete
    /// đã dùng cho các danh mục khác (Line/Stage), phục vụ NFR "khả năng mở rộng" (thêm/vô hiệu hóa trạm
    /// chỉ bằng cấu hình, không cần deploy lại).
    /// </summary>
    public bool IsActive { get; set; } = true;
}
