using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// Event log sự kiện máy thông điện theo trạm (US-14 AC12). Ghi từ Station.Wpf qua <c>POST api/v1/tester/events</c>
/// (StationApiKey); nếu không ghi được (mất server) thì Station.Wpf chỉ ghi file Serilog và KHÔNG chặn thao tác. Log
/// serial vào/ra KHÔNG nằm ở đây (chỉ ghi file, AC11).
/// </summary>
/// <remarks>Không dùng khoá ngoại ở DB (CLAUDE.md) — <see cref="WorkStationId"/> là cột tham chiếu thuần.</remarks>
public class TesterEventLog
{
    public int Id { get; set; }

    public int WorkStationId { get; set; }

    /// <summary>Thời điểm ghi nhận — giờ local server (API-Conventions.md mục 10), tên giữ "...AtUtc" theo quy ước đặt tên đã chốt.</summary>
    public DateTime OccurredAtUtc { get; set; }

    public TesterEventType EventType { get; set; }

    /// <summary>Mã tem liên quan (vd SCAN_ON_NG) — null nếu không có.</summary>
    public string? TagCode { get; set; }

    /// <summary>SEQ kết quả của máy liên quan — null nếu không có.</summary>
    public int? TesterSeq { get; set; }

    /// <summary>Chi tiết tự do (lý do, GM, thông báo lỗi...).</summary>
    public string? Detail { get; set; }

    /// <summary>Người duyệt (vd Tổ trưởng hủy kết quả — VOID_DISCARD) — null nếu không có.</summary>
    public string? ApprovedByUserName { get; set; }
}
