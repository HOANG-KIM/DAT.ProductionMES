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
    /// "Trạm có máy thông điện" (FR-11/US-11, viết lại 12/10/2026) — về mặt vật lý trạm có gắn máy thông điện
    /// ESP32; do Admin đặt. Chỉ trạm có cờ này mới chạy luồng "thông điện trước, quét QR sau" (US-12) và mới
    /// hiện công tắc chế độ. Khác <see cref="TesterEnabled"/> (công tắc runtime do Tổ trưởng đổi).
    /// </summary>
    public bool UseArduino { get; set; }

    /// <summary>
    /// Công tắc runtime "Đang sử dụng máy thông điện" (US-11 AC10) — lưu trên server (không nằm trong
    /// appsettings.json trạm) để mọi lần đổi đều đi qua API có log. Mặc định <c>true</c> (BẬT) khi trạm mới. Chỉ
    /// có ý nghĩa khi <see cref="UseArduino"/> = true: TẮT = Bypass (<c>Scan.TesterMode = Bypass</c>).
    /// </summary>
    public bool TesterEnabled { get; set; } = true;

    /// <summary>[US-11 AC12] KHÔNG còn dùng để kết nối — cấu hình COM nay ở appsettings.json cục bộ (section Tester). Giữ cột để tương thích dữ liệu cũ.</summary>
    public string? ComPort { get; set; }

    /// <summary>[US-11 AC12] KHÔNG còn dùng — xem <see cref="ComPort"/>.</summary>
    public int? BaudRate { get; set; }

    /// <summary>[US-11 AC12] KHÔNG còn dùng — xem <see cref="ComPort"/>.</summary>
    public string? CommandProtocol { get; set; }

    /// <summary>
    /// Trạng thái hoạt động của trạm. Mặc định <c>true</c> khi tạo mới — theo cùng pattern soft-delete
    /// đã dùng cho các danh mục khác (Line/Stage), phục vụ NFR "khả năng mở rộng" (thêm/vô hiệu hóa trạm
    /// chỉ bằng cấu hình, không cần deploy lại).
    /// </summary>
    public bool IsActive { get; set; } = true;
}
