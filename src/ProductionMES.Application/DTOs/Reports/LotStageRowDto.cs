namespace ProductionMES.Application.DTOs.Reports;

/// <summary>
/// 1 dòng (Line, Công đoạn) trong chi tiết 1 Lot (US-21 AC4) — khác <see cref="ProductionReportRowDto"/> (bản
/// vòng 2, group theo (Line, Công đoạn, Lot) kèm PLAN/BALANCE): dòng này KHÔNG có PLAN/BALANCE (AC6 — tạm hoãn),
/// chỉ đếm OK/NG snapshot trực tiếp trên <c>Scan.Lot</c> (US-10 AC4), phạm vi ĐÃ CỐ ĐỊNH đúng 1 Lot nên không cần
/// gộp (SUM) nhiều <c>ProductionPlan</c> như bản vòng 2.
/// </summary>
public class LotStageRowDto
{
    public int LineId { get; set; }

    public string LineName { get; set; } = string.Empty;

    public int StageId { get; set; }

    public string StageName { get; set; } = string.Empty;

    /// <summary>Số lượng OK (<c>Scan.Result = Ok</c>) trong khoảng thời gian đang xem (AC5).</summary>
    public int OkCount { get; set; }

    /// <summary>Số lượng NG (<c>Scan.Result = Ng</c>, KHÔNG tính DuplicateTag/PreviousStageNotPassed/WaitingReworkUnlock — cùng quy tắc đã chốt ở US-09/US-18) trong khoảng thời gian đang xem (AC5).</summary>
    public int NgCount { get; set; }

    /// <summary>
    /// US-21a AC5 — "Đủ"/"Chưa đủ" so <see cref="OkCount"/> với "Tổng số lượng Lot" (<see cref="LotSummaryDto.LotTotalQuantity"/>,
    /// nhập tay, KHÔNG phải PlannedQuantity của kế hoạch). <c>null</c> khi <see cref="LotSummaryDto.LotTotalQuantity"/>
    /// = <c>null</c> ("Chưa xác định" — AC6, không suy luận sai); <c>true</c> khi <c>OkCount >= LotTotalQuantity</c>.
    /// So sánh theo TỪNG dòng riêng biệt (đã chốt với Ban quản lý), KHÔNG gộp thành 1 con số duy nhất cho cả Lot.
    /// </summary>
    public bool? IsSufficientQuantity { get; set; }
}
