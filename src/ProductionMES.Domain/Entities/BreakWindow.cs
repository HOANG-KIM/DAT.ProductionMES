namespace ProductionMES.Domain.Entities;

/// <summary>
/// Khung giờ nghỉ cấu hình theo Line (FR-01/FR-09a/US-01a) — dùng làm cơ sở trừ đúng thời gian nghỉ khi tính
/// sản lượng kế hoạch lũy kế hiển thị tại màn hình trạm (US-09 AC5/AC6). Áp dụng chung cho <b>mọi</b> kế hoạch
/// sản xuất chạy trên Line đó, không cấu hình riêng theo từng kế hoạch (AC1).
/// </summary>
/// <remarks>
/// 1 Line có thể có 0..N <see cref="BreakWindow"/> (AC2/AC4) — quan hệ 1-N thuần túy qua <see cref="LineId"/>,
/// không cần navigation property 2 chiều ở giai đoạn này (cùng lý do đã áp dụng cho <c>WorkStation.LineId</c>).
/// <see cref="StartTime"/>/<see cref="EndTime"/> dùng <see cref="TimeOnly"/> (không phải <see cref="DateTime"/>)
/// vì đây thuần túy là giờ trong ngày lặp lại hàng ngày (vd 12:00–13:00), không gắn với 1 ngày cụ thể nào —
/// EF Core 8 + Pomelo.EntityFrameworkCore.MySql 8.0.2 map <see cref="TimeOnly"/> trực tiếp sang cột MySQL kiểu
/// <c>TIME</c>, không cần kiểu trung gian <see cref="TimeSpan"/>.
/// </remarks>
public class BreakWindow
{
    public int Id { get; set; }

    /// <summary>Line áp dụng khung giờ nghỉ này (AC1).</summary>
    public int LineId { get; set; }

    /// <summary>Giờ bắt đầu nghỉ (AC1).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Giờ kết thúc nghỉ — phải lớn hơn <see cref="StartTime"/> (AC5).</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>Ghi chú (vd "Nghỉ trưa", "Nghỉ giữa giờ") — không bắt buộc (AC1).</summary>
    public string? Note { get; set; }
}
