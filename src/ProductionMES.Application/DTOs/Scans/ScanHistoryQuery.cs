using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Bộ lọc tra cứu lịch sử scan (US-10 AC2/AC3; mở rộng 18/08/2026 phục vụ US-21 AC7 drill-down). Mọi field lọc
/// đều tùy chọn (<c>null</c> = không áp dụng) và kết hợp theo AND — vd truyền cả <see cref="WorkStationId"/> lẫn
/// <see cref="FromUtc"/>/<see cref="ToUtc"/> thì trả về đúng giao của 2 điều kiện, đúng AC3 ("có thể kết hợp").
/// </summary>
public class ScanHistoryQuery
{
    /// <summary>AC2 — tra theo đúng 1 mã tem. Không phân biệt hoa/thường theo đúng dữ liệu đã lưu (so khớp tuyệt đối).</summary>
    public string? TagCode { get; set; }

    /// <summary>AC3 — lọc theo trạm làm việc thực hiện lượt scan.</summary>
    public int? WorkStationId { get; set; }

    /// <summary>AC3 — lọc theo Line (ngữ cảnh lượt scan, xem remarks tại entity Scan).</summary>
    public int? LineId { get; set; }

    /// <summary>Mở rộng 18/08/2026 (US-21 AC7) — lọc theo công đoạn (Stage).</summary>
    public int? StageId { get; set; }

    /// <summary>Mở rộng 18/08/2026 (US-21 AC7) — lọc theo Lot (snapshot bất biến, US-10 AC4).</summary>
    public string? Lot { get; set; }

    /// <summary>Mở rộng 18/08/2026 (US-21 AC6) — lọc theo Model (snapshot bất biến, US-10 AC4).</summary>
    public string? Model { get; set; }

    /// <summary>Mở rộng 18/08/2026 (US-21 AC6) — lọc theo Khách hàng (snapshot bất biến, US-10 AC4).</summary>
    public string? Customer { get; set; }

    /// <summary>Mở rộng 18/08/2026 (US-21 AC6) — lọc theo Revision (snapshot bất biến, US-10 AC4).</summary>
    public string? Revision { get; set; }

    /// <summary>
    /// Mở rộng 19/08/2026 (nâng cấp UX drill-down US-21 AC7-AC11) — lọc theo kết quả scan (vd chỉ xem
    /// <see cref="ScanResult.Ok"/> hoặc <see cref="ScanResult.Ng"/>). <c>null</c> = không áp dụng, trả về mọi kết quả.
    /// </summary>
    public ScanResult? Result { get; set; }

    /// <summary>AC3 — cận dưới khoảng thời gian (bao gồm), so trên <c>Scan.ScannedAtUtc</c>.</summary>
    public DateTime? FromUtc { get; set; }

    /// <summary>AC3 — cận trên khoảng thời gian (bao gồm), so trên <c>Scan.ScannedAtUtc</c>.</summary>
    public DateTime? ToUtc { get; set; }

    /// <summary>Trang hiện tại, bắt đầu từ 1 (`Documents/API-Conventions.md` mục 9). Giá trị &lt; 1 được Service tự chỉnh về 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số bản ghi/trang. Giá trị &lt; 1 hoặc &gt; 200 được Service tự chỉnh về mặc định 20 (chống lạm dụng lấy toàn bộ dữ liệu 1 lần).</summary>
    public int PageSize { get; set; } = 20;
}
