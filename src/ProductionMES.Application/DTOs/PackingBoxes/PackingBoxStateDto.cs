namespace ProductionMES.Application.DTOs.PackingBoxes;

/// <summary>
/// Trạng thái đóng thùng hiện tại của (Line, Công đoạn "Đóng thùng") tại 1 trạm (US-25 AC5/AC6/AC9) — Station.Wpf
/// gọi khi khởi động màn hình/quay lại màn hình để tự khôi phục đúng số thùng + số lượng đang dở, không yêu cầu
/// nhập lại BoxNo (AC6).
/// </summary>
public class PackingBoxStateDto
{
    /// <summary>true nếu kế hoạch hiện tại CHƯA từng có thùng nào được mở tại công đoạn này — AC5: phải nhập BoxNo bắt đầu trước khi cho quét tem đầu tiên.</summary>
    public bool RequiresStartingBoxNo { get; set; }

    /// <summary>Thùng đang mở (InProgress) hiện tại — null khi <see cref="RequiresStartingBoxNo"/> = true.</summary>
    public PackingBoxDto? CurrentBox { get; set; }

    /// <summary>Thùng vừa hoàn tất gần nhất (nếu có) — dùng cho thao tác "In lại" (AC13) không cần nhớ Id thùng cũ.</summary>
    public PackingBoxDto? LastCompletedBox { get; set; }
}
