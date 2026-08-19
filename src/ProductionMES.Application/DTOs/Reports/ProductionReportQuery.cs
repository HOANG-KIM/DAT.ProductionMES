namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// Bộ lọc báo cáo tổng hợp theo Line/Lot/công đoạn (US-21, FR-21 — mở rộng 18/08/2026, AC4-AC8).
/// </summary>
/// <remarks>
/// <see cref="FromUtc"/>/<see cref="ToUtc"/> đều KHÔNG truyền (null) → AC1 "xem theo thời gian thực": mỗi dòng
/// tính ACTUAL/PLAN/BALANCE lũy kế từ lúc kế hoạch đang <c>Running</c> của (Line, Công đoạn) đó bắt đầu chạy cho
/// tới hiện tại — giống hệt cách <c>AndonBoardService</c> (US-09) đã làm cho 1 trạm, chỉ khác là tổng hợp cho MỌI
/// cặp (Line, Công đoạn) cùng lúc.
///
/// Có truyền ít nhất 1 trong 2 field → AC2 "xem theo khoảng thời gian đã qua": ACTUAL/NG chỉ đếm lượt scan có
/// <c>ScannedAtUtc</c> nằm trong [FromUtc, ToUtc] (thiếu 1 đầu coi như không giới hạn đầu đó); PLAN cộng dồn
/// <c>ComputePlanCumulative</c> của TỪNG kế hoạch khớp (Line, Công đoạn, Lot) trong đúng khoảng — xem remarks tại
/// <c>ProductionReportService</c>.
///
/// <see cref="StageId"/>/<see cref="Lot"/>/<see cref="Model"/>/<see cref="Customer"/>/<see cref="Revision"/> (AC6,
/// mới 18/08/2026) đều tùy chọn, kết hợp AND với nhau và với <see cref="LineId"/>/khoảng thời gian — so khớp tuyệt
/// đối trên field tương ứng của <c>ProductionPlan</c> (Lot/Model/Customer/Revision là dữ liệu cấp kế hoạch, bị
/// khóa tuyệt đối sau lượt scan đầu tiên — mục 6 quy tắc 14 SRS — nên tương đương snapshot trên <c>Scan</c>).
/// </remarks>
public class ProductionReportQuery
{
    /// <summary>Lọc theo đúng 1 Line — tùy chọn, không truyền = tất cả Line có trạm làm việc đang hoạt động.</summary>
    public int? LineId { get; set; }

    /// <summary>AC6 (mới) — lọc theo đúng 1 công đoạn (Stage) — tùy chọn.</summary>
    public int? StageId { get; set; }

    /// <summary>AC6 (mới) — lọc theo đúng 1 Lot — tùy chọn.</summary>
    public string? Lot { get; set; }

    /// <summary>AC6 (mới) — lọc theo Model sản phẩm — tùy chọn.</summary>
    public string? Model { get; set; }

    /// <summary>AC6 (mới) — lọc theo Khách hàng — tùy chọn.</summary>
    public string? Customer { get; set; }

    /// <summary>AC6 (mới) — lọc theo Revision — tùy chọn.</summary>
    public string? Revision { get; set; }

    /// <summary>AC2 — cận dưới (bao gồm) khoảng thời gian xem báo cáo, UTC. Null = không giới hạn đầu dưới.</summary>
    public DateTime? FromUtc { get; set; }

    /// <summary>AC2 — cận trên (bao gồm) khoảng thời gian xem báo cáo, UTC. Null = tính tới hiện tại (AC1 nếu FromUtc cũng null).</summary>
    public DateTime? ToUtc { get; set; }
}
