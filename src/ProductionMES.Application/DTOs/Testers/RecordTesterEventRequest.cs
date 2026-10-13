using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Testers;

/// <summary>
/// Request ghi 1 sự kiện máy thông điện (US-14 AC12) — <c>POST api/v1/tester/events</c>, StationApiKey (WorkStationId lấy
/// từ claim API key, không nhận từ body).
/// </summary>
public class RecordTesterEventRequest
{
    public TesterEventType EventType { get; set; }

    public string? TagCode { get; set; }

    public int? TesterSeq { get; set; }

    public string? Detail { get; set; }

    /// <summary>Người duyệt (vd Tổ trưởng hủy kết quả) do Station.Wpf lấy từ phiên đăng nhập nâng quyền tại trạm.</summary>
    public string? ApprovedByUserName { get; set; }
}
