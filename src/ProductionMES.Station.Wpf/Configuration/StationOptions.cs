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

    /// <summary>
    /// Id thật của <c>WorkStation</c> tại trạm này (US-07) — gửi trong body <c>CreateScanRequest.WorkStationId</c>
    /// để server đối chiếu chống giả danh (ADR-005 AC6); nguồn xử lý business logic thật vẫn là claim từ API key
    /// đã xác thực, giá trị này chỉ cần khớp đúng để không bị 401. Cấu hình cục bộ theo trạm, giống LineId/StageId.
    /// </summary>
    public int WorkStationId { get; set; }

    /// <summary>
    /// Giá trị thô của API Key theo trạm (ADR-005/US-04a) — gửi ở header <c>X-Station-Api-Key</c> cho scheme
    /// <c>StationApiKey</c>. Giá trị thô chỉ được server trả về 1 lần lúc Admin issue/reissue qua web-admin
    /// (<c>StationApiKeysController</c>), sau đó không phục hồi lại được từ DB (chỉ lưu KeyHash) — phải copy thủ
    /// công vào đây. Đây là cấu hình bí mật, không commit giá trị thật (chỉ commit placeholder rỗng).
    /// </summary>
    public string StationApiKeyValue { get; set; } = string.Empty;

    /// <summary>
    /// Cờ cấu hình cục bộ theo trạm (US-07, quyết định 17/08/2026 — SRS mục 5.1/8.2) cho "chế độ nhập tay/test":
    /// mặc định <c>false</c> ở mọi trạm (bao gồm production). Khi <c>true</c>, <c>AndonBoardWindow</c> hiển thị
    /// thêm 1 khu vực nhập tay mã tem riêng biệt kèm banner cảnh báo liên tục, dùng khi kiểm thử/demo chưa có
    /// máy quét vật lý. KHÔNG phải cơ chế chặn kỹ thuật: vì máy scan hoạt động theo keyboard-wedge (giả lập bàn
    /// phím), input từ máy quét thật vẫn luôn được nhận qua <c>ScanInputBox</c> bất kể cờ này bật/tắt — cờ chỉ
    /// nhằm giảm rủi ro vô tình lẫn dữ liệu giả vào production bằng cách bắt buộc bật rõ ràng + cảnh báo trực quan.
    /// </summary>
    public bool EnableManualScanInput { get; set; }

    /// <summary>
    /// Chu kỳ (giây) gọi lại <c>GET api/v1/andon-board</c> để làm mới toàn bộ bảng PLAN/ACTUAL/BALANCE (US-09
    /// AC4/AC6) — dùng cho việc PLAN "trôi" theo thời gian (không phụ thuộc lượt scan) và phát hiện mốc giờ mới
    /// xuất hiện. Tăng ACTUAL theo lượt scan mới KHÔNG dùng chu kỳ này — tái sử dụng trực tiếp sự kiện SignalR
    /// <c>ScanRecorded</c> đã có (<c>AndonBoardViewModel.OnScanRecorded</c>) để đạt độ trễ ≤ 1s (AC4). Mặc định
    /// 30 giây — đủ mượt để PLAN không bị "đứng hình" lâu mà không gọi server quá dày.
    /// </summary>
    public int AndonBoardRefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Thời gian (giây) không có tương tác nào (chuột/bàn phím) tại các trang chế độ Tổ trưởng dùng chung phiên
    /// đăng nhập (<c>PlanSettingsPage</c>/<c>PlanSelectionPage</c>/<c>LineStageSequencePage</c>/
    /// <c>StationConfigPage</c> — ADR-006) trước khi tự động clear session (<c>ISupervisorSessionService.Clear()</c>)
    /// và quay về Trang chủ. Là idle timer thật (reset lại mỗi khi có tương tác, không phải đếm cố định kể từ lúc
    /// đăng nhập) — tránh đá Tổ trưởng ra giữa chừng khi đang nhập dở form. Cấu hình cục bộ theo trạm, cùng nhóm
    /// với <see cref="ArduinoTimeoutSeconds"/>/<see cref="NgModeTimeoutSeconds"/>. Mặc định 600 giây (10 phút).
    /// KHÔNG áp dụng cho <c>ReworkUnlockPage</c> (US-19 AC7 đã bắt buộc đăng nhập lại mỗi lần vào, không dùng
    /// <c>ISupervisorSessionService</c> dùng chung).
    /// </summary>
    public int SupervisorSessionIdleTimeoutSeconds { get; set; } = 600;
}
