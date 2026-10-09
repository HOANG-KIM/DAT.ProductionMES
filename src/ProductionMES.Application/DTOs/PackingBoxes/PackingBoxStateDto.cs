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

    /// <summary>Model của kế hoạch đang chạy — hiển thị kèm gợi ý số thùng (dãy số thùng tính theo (Model, Lot)).</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Lot của kế hoạch đang chạy — hiển thị kèm gợi ý số thùng.</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>
    /// Số thùng gợi ý cho lần nhập số thùng bắt đầu (AC5) hoặc sửa số thùng (AC7) = số thùng lớn nhất đã dùng của
    /// cùng (Model, Lot) qua MỌI kế hoạch + 1, không tính thùng đang mở của kế hoạch hiện tại; chưa có thùng nào thì 1.
    /// </summary>
    public int SuggestedNextBoxNo { get; set; } = 1;

    /// <summary>Tổng số lượng Lot (nhập tay, US-21a) — null = Lot chưa có, trạm Đóng thùng bị chặn cho tới khi nhập.</summary>
    public int? LotTotalQuantity { get; set; }

    /// <summary>Số sản phẩm đã quét vào thùng của cùng (Model, Lot) — gồm cả thùng đang mở.</summary>
    public int LotPackedQuantity { get; set; }

    /// <summary>Tổng số thùng (= số tem) của Lot = làm tròn lên (Tổng số lượng Lot / Quy cách đóng gói) — null nếu thiếu 1 trong 2.</summary>
    public int? LotTotalBoxCount { get; set; }

    /// <summary>Số thùng đã đóng xong (đã in tem) của cùng (Model, Lot).</summary>
    public int LotCompletedBoxCount { get; set; }

    /// <summary>true khi các thùng đã đóng xong đủ Tổng số lượng Lot — không mở thùng mới, chặn quét thêm.</summary>
    public bool IsLotFullyPacked { get; set; }
}
