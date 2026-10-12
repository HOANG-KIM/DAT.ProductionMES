namespace ProductionMES.Station.Wpf.Configuration;

/// <summary>Cách gửi file tem thùng (.xlsx) tới máy in tại trạm (US-28).</summary>
public enum LabelPrintEngine
{
    /// <summary>Dùng Microsoft Excel nếu trạm có cài, không có thì chuyển sang LibreOffice.</summary>
    Auto,

    /// <summary>Chỉ dùng Microsoft Excel (COM) — báo lỗi nếu trạm chưa cài Excel.</summary>
    Excel,

    /// <summary>Chỉ dùng LibreOffice (<c>soffice --headless</c>) — báo lỗi nếu không tìm thấy <c>soffice.exe</c>.</summary>
    LibreOffice,
}

/// <summary>
/// Cấu hình in tem thùng — section <c>LabelPrint</c> trong <c>appsettings.json</c> của từng trạm (US-28). Cấu hình
/// cục bộ theo trạm (không phải server) vì phụ thuộc phần mềm/máy in thực tế cài trên từng máy, cùng nguyên tắc
/// với <see cref="StationOptions.NgModeTimeoutSeconds"/> (CLAUDE.md mục "Cấu hình"). Luôn in ra máy in mặc định của
/// Windows — chọn máy in tem bằng cách đặt nó làm mặc định trên máy trạm, không cấu hình tên máy in ở đây.
/// </summary>
public class LabelPrintOptions
{
    public LabelPrintEngine Engine { get; set; } = LabelPrintEngine.Auto;

    /// <summary>Đường dẫn đầy đủ tới <c>soffice.exe</c>. Để trống = tự dò trong thư mục cài đặt mặc định của LibreOffice.</summary>
    public string LibreOfficePath { get; set; } = string.Empty;

    /// <summary>Thời gian tối đa (giây) chờ 1 lệnh in hoàn tất trước khi coi là lỗi — chặn trường hợp Excel/LibreOffice treo.</summary>
    public int TimeoutSeconds { get; set; } = 60;
}
