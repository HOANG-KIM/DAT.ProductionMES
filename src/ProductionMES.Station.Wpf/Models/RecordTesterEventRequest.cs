using ProductionMES.Station.Tester.Station;

namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>RecordTesterEventRequest</c> phía backend (US-14 AC12) — <c>POST api/v1/tester/events</c>.
/// <see cref="EventType"/> khớp TÊN với <c>TesterEventType</c> phía backend (JSON dạng chuỗi).
/// </summary>
public class RecordTesterEventRequest
{
    public TesterEventType EventType { get; set; }

    public string? TagCode { get; set; }

    public int? TesterSeq { get; set; }

    public string? Detail { get; set; }

    public string? ApprovedByUserName { get; set; }
}
