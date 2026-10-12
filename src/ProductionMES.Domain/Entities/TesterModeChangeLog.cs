using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// Log mỗi lần Tổ trưởng đổi công tắc "Đang sử dụng máy thông điện" tại trạm (US-11 AC4): trạm, từ chế độ, đến
/// chế độ, lý do, người duyệt, thời điểm. Chỉ ghi khi đổi thành công — đổi bị từ chối (AC5) không ghi gì.
/// </summary>
/// <remarks>
/// <see cref="FromMode"/>/<see cref="ToMode"/> chỉ nhận <see cref="TesterMode.Tester"/> (công tắc BẬT) hoặc
/// <see cref="TesterMode.Bypass"/> (công tắc TẮT). Không dùng khoá ngoại ở DB (CLAUDE.md) — <see cref="WorkStationId"/>
/// là cột tham chiếu thuần.
/// </remarks>
public class TesterModeChangeLog
{
    public int Id { get; set; }

    public int WorkStationId { get; set; }

    /// <summary>Thời điểm đổi — giờ local hệ thống (cùng quy ước <c>Scan.ScannedAtUtc</c>, API-Conventions.md mục 10).</summary>
    public DateTime ChangedAtUtc { get; set; }

    public TesterMode FromMode { get; set; }

    public TesterMode ToMode { get; set; }

    /// <summary>Lý do đổi (bắt buộc) — chọn từ gợi ý hoặc tự gõ.</summary>
    public string Reason { get; set; } = string.Empty;

    public int ApprovedByUserId { get; set; }

    /// <summary>Tên đăng nhập của <see cref="ApprovedByUserId"/> tại thời điểm đổi (snapshot).</summary>
    public string ApprovedByUserName { get; set; } = string.Empty;
}
