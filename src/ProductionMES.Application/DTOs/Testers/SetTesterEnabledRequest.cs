namespace ProductionMES.Application.DTOs.Testers;

/// <summary>
/// Request đổi công tắc "Đang sử dụng máy thông điện" (US-11 AC4) — <c>PUT api/v1/tester/enabled</c>, Bearer
/// Tổ trưởng + permission <c>TesterMode.Change</c>. Người duyệt lấy từ claim JWT, KHÔNG nhận từ body.
/// </summary>
public class SetTesterEnabledRequest
{
    /// <summary>Trạm cần đổi — Station.Wpf tự khai từ cấu hình cục bộ (cùng pattern <c>CreateNgScanRequest.WorkStationId</c>).</summary>
    public int WorkStationId { get; set; }

    /// <summary><c>true</c> = BẬT (dùng máy), <c>false</c> = TẮT (Bypass).</summary>
    public bool Enabled { get; set; }

    /// <summary>Lý do đổi (bắt buộc).</summary>
    public string Reason { get; set; } = string.Empty;
}
