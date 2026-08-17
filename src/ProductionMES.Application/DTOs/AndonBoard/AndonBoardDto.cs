namespace ProductionMES.Application.DTOs.AndonBoard;

/// <summary>
/// Dữ liệu bảng PLAN/ACTUAL/BALANCE theo mốc giờ tại màn hình trạm (US-09, FR-09/FR-09a) — luôn trả về đúng 1
/// trạm (WorkStationId lấy từ danh tính API Key đã xác thực, xem <c>AndonBoardController</c>, cùng nguyên tắc
/// với <c>ScansController</c> theo ADR-005).
/// </summary>
public class AndonBoardDto
{
    public int WorkStationId { get; set; }

    public int LineId { get; set; }

    public int StageId { get; set; }

    /// <summary>
    /// False khi (Line, Công đoạn) của trạm hiện KHÔNG có <c>ProductionPlanStage</c> nào đang <c>Running</c> —
    /// mọi field số liệu bên dưới sẽ là 0/rỗng, UI hiển thị trạng thái "chưa có kế hoạch active" thay vì lỗi.
    /// Khác với luồng scan (<c>ScanService</c>) — endpoint này chỉ HIỂN THỊ nên không ném lỗi cho trường hợp này.
    /// </summary>
    public bool HasActivePlan { get; set; }

    public int? ProductionPlanId { get; set; }

    /// <summary>AC1: số lượng đã scan OK lũy kế đến hiện tại (đúng cặp Kế hoạch, Công đoạn của trạm).</summary>
    public int ActualCumulative { get; set; }

    /// <summary>AC1/AC5: sản lượng kế hoạch lũy kế đến hiện tại — đã trừ phần giao với khung giờ nghỉ (FR-09a).</summary>
    public int PlanCumulative { get; set; }

    /// <summary>AC1/AC2/AC3: chênh lệch = ActualCumulative − PlanCumulative. Dương → xanh, âm → đỏ (UI quyết định màu).</summary>
    public int Balance { get; set; }

    /// <summary>
    /// Tổng lượt scan có kết quả khác Ok (DuplicateTag/PreviousStageNotPassed) lũy kế TOÀN CA — gộp 1 chỉ số duy
    /// nhất cho cả bảng, không tách theo từng mốc giờ (quyết định layout 13/08/2026).
    /// </summary>
    public int NgCount { get; set; }

    /// <summary>%NG = NgCount / (NgCount + ActualCumulative) × 100, làm tròn 1 chữ số thập phân. 0 khi chưa có lượt scan nào.</summary>
    public decimal NgPercent { get; set; }

    /// <summary>Mốc "hiện tại" (giờ tường tại nhà máy) dùng làm cơ sở tính toàn bộ số liệu ở trên — phục vụ debug, UI không bắt buộc dùng trực tiếp.</summary>
    public DateTime GeneratedAtLocal { get; set; }

    /// <summary>Các dòng theo mốc giờ (AC6) — phần tử cuối cùng luôn là dòng "hiện tại" (<see cref="AndonBoardHourRowDto.IsCurrent"/> = true).</summary>
    public IReadOnlyList<AndonBoardHourRowDto> Rows { get; set; } = Array.Empty<AndonBoardHourRowDto>();
}
