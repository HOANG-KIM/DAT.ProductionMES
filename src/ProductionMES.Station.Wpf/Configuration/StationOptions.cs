namespace ProductionMES.Station.Wpf.Configuration;

/// <summary>
/// Cấu hình cục bộ tại từng trạm, đọc từ <c>appsettings.json</c> của chính trạm đó (Options pattern, không đọc
/// <c>IConfiguration</c> rải rác) — cùng quy ước với <see cref="ArduinoTimeoutSeconds"/>/<see cref="NgModeTimeoutSeconds"/>
/// đã có trước đây (xem CLAUDE.md mục "Cấu hình").
/// </summary>
public class StationOptions
{
    public const string SectionName = "";

    public string ApiBaseUrl { get; set; } = string.Empty;

    public string SignalRHubUrl { get; set; } = string.Empty;

    public int ArduinoTimeoutSeconds { get; set; } = 45;

    public int NgModeTimeoutSeconds { get; set; } = 30;

    public string WorkStationName { get; set; } = string.Empty;

    /// <summary>
    /// Line + Công đoạn cố định của trạm này, cấu hình cục bộ tại trạm — Station.Wpf hiện chưa có endpoint tra
    /// cứu WorkStation theo API key để tự suy ra 2 giá trị này, nên tạm cấu hình trực tiếp (đơn giản, đúng tinh
    /// thần "cấu hình cục bộ theo trạm" đã có). Tên hiển thị (LineName/StageName) cần khớp thủ công với Id khi
    /// đổi cấu hình — cải tiến sau: gọi API tra cứu WorkStation để tự đồng bộ.
    /// </summary>
    public int LineId { get; set; }

    public string LineName { get; set; } = string.Empty;

    public int StageId { get; set; }

    public string StageName { get; set; } = string.Empty;
}
