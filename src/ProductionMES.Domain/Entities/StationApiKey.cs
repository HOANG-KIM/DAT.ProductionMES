namespace ProductionMES.Domain.Entities;

/// <summary>
/// API Key riêng cho từng trạm làm việc (<see cref="WorkStation"/>), dùng để <c>Station.Wpf</c> xác thực vào
/// luồng scan thường qua <c>AuthenticationScheme "StationApiKey"</c> (ADR-005, US-04a) — Operator không đăng
/// nhập cá nhân, trạm là đơn vị xác thực.
/// </summary>
/// <remarks>
/// Tách riêng thành entity độc lập (không chỉ thêm field vào <see cref="WorkStation"/>) để giữ lại lịch sử các
/// key cũ đã thu hồi khi xoay vòng (ADR-005 dòng 26, 83) — phục vụ truy vết (vd. key nào đang active tại thời
/// điểm xảy ra sự cố). <see cref="KeyHash"/> lưu SHA-256 (hex) của giá trị thô, cùng nguyên tắc với
/// <see cref="RefreshToken.TokenHash"/> (ADR-003) — server không bao giờ lưu giá trị thô, giá trị thô chỉ trả
/// về đúng 1 lần ngay lúc cấp/cấp lại (AC1/AC2/AC4).
/// </remarks>
public class StationApiKey
{
    public int Id { get; set; }

    /// <summary>Trạm sở hữu key này.</summary>
    public int WorkStationId { get; set; }

    /// <summary>SHA-256 (hex) của API key thô — dùng để so khớp khi xác thực (AC5/AC6).</summary>
    public string KeyHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Thời điểm bị thu hồi (UTC), <c>null</c> nếu còn hoạt động (Active). Bị set khi Admin chủ động thu hồi
    /// (AC3) hoặc khi xoay vòng/cấp lại key mới cho cùng trạm (AC4) — không có cơ chế hết hạn tự động theo
    /// thời gian (khác access token 15 phút của ADR-003).
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }
}
