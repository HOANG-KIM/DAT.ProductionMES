namespace ProductionMES.Domain.Enums;

/// <summary>
/// Chế độ của một lượt scan đối với máy thông điện ESP32 (US-11/US-12). Lưu trên <c>Scan.TesterMode</c>, cho phép
/// báo cáo/tra cứu lọc riêng theo từng chế độ.
/// </summary>
public enum TesterMode
{
    /// <summary>Trạm không có máy thông điện (<c>WorkStation.UseArduino = false</c>) — luồng scan thường, mặc định.</summary>
    None = 0,

    /// <summary>Trạm có máy, công tắc "Đang sử dụng máy thông điện" BẬT — kết quả OK đã được máy xác nhận (BIND_OK) và gắn với tem (US-12).</summary>
    Tester = 1,

    /// <summary>Trạm có máy nhưng công tắc TẮT (US-11 AC6): vẫn lưu <c>Result = Ok</c>, tính là đã qua công đoạn, đánh dấu để phân biệt khi báo cáo.</summary>
    Bypass = 2,
}
