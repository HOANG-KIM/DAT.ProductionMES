namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// 1 dòng báo cáo tổng hợp cho đúng 1 bộ (Line, Lot, Công đoạn) (US-21 AC4 — mở rộng 18/08/2026 từ (Line, Công
/// đoạn) sang thêm dimension Lot).
/// </summary>
public class ProductionReportRowDto
{
    public int LineId { get; set; }

    public string LineName { get; set; } = string.Empty;

    public int StageId { get; set; }

    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// AC4 — Lot xác định dòng này. Rỗng ("") khi <see cref="HasPlanData"/> = false (chưa từng có kế hoạch nào
    /// cấu hình cho (Line, Công đoạn) này, nên chưa xác định được Lot).
    /// </summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>
    /// True khi có ít nhất 1 kế hoạch sản xuất làm cơ sở tính PLAN/ACTUAL/NG cho dòng này. False → không có kế
    /// hoạch nào tham chiếu được (hoặc AC1: không có kế hoạch nào đang <c>Running</c>), mọi số liệu = 0 — xem
    /// remarks tại <c>ProductionReportService</c>.
    /// </summary>
    public bool HasPlanData { get; set; }

    /// <summary>
    /// Id TẤT CẢ kế hoạch sản xuất đã được GỘP (SUM) vào dòng này (Quyết định 18/08/2026: cùng (Line, Công đoạn,
    /// Lot) nhưng khác <c>ProductionPlan.Id</c> — vd Lot bị tạo lại thành kế hoạch mới sau khi kế hoạch cũ
    /// <c>Cancelled</c> — đều cộng dồn chung 1 dòng). Rỗng khi <see cref="HasPlanData"/> = false.
    /// </summary>
    public IReadOnlyList<int> ProductionPlanIds { get; set; } = Array.Empty<int>();

    /// <summary>ACTUAL — số lượng đã scan OK (US-21 AC3/AC5: chỉ tính lượt <c>ScanResult.Ok</c>, đã xác nhận, không tính "Chờ đồng bộ").</summary>
    public int Actual { get; set; }

    /// <summary>
    /// AC5 (mới) — số lượng NG, chỉ đếm <c>ScanResult.Ng</c> (KHÔNG tính <c>DuplicateTag</c>/
    /// <c>PreviousStageNotPassed</c>/<c>WaitingReworkUnlock</c> — cùng quy tắc NG/%NG đã chốt ở US-09/US-18).
    /// </summary>
    public int NgCount { get; set; }

    /// <summary>PLAN — sản lượng kế hoạch tương ứng cùng khoảng thời gian, đã trừ khung giờ nghỉ theo Line (tái dùng công thức US-09/FR-09a), cộng dồn qua mọi kế hoạch trong <see cref="ProductionPlanIds"/>.</summary>
    public int Plan { get; set; }

    /// <summary>BALANCE = Actual − Plan. Dương → vượt kế hoạch, âm → chậm kế hoạch.</summary>
    public int Balance { get; set; }
}
