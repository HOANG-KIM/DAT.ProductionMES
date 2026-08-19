using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// 1 dòng kết quả tra cứu lịch sử scan (US-10 AC2/AC3). Giữ đủ field snapshot bất biến
/// (<see cref="Customer"/>/<see cref="Model"/>/<see cref="Lot"/>/<see cref="Revision"/>/
/// <see cref="PlannedQuantity"/>/<see cref="TaktTimeSeconds"/>) đúng như đã lưu tại thời điểm scan — KHÔNG tra
/// cứu động qua <see cref="ProductionPlanId"/> (AC4, xem remarks tại entity <c>Scan</c>).
/// </summary>
public class ScanHistoryItemDto
{
    public int Id { get; set; }

    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int LineId { get; set; }

    public int WorkStationId { get; set; }

    /// <summary>US-21 AC8: tên trạm làm việc đã thực hiện lượt scan này — KHÔNG phải tên cá nhân Operator (ADR-005, mục 8.2 SRS). Rỗng nếu WorkStationId không còn tồn tại (hiếm, dữ liệu cũ).</summary>
    public string WorkStationName { get; set; } = string.Empty;

    public int ProductionPlanId { get; set; }

    /// <summary>Snapshot ProductionPlan.Customer tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Model tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Lot tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Revision tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public string? Revision { get; set; }

    /// <summary>Snapshot ProductionPlan.PlannedQuantity tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>Snapshot ProductionPlan.TaktTimeSeconds tại thời điểm scan (US-10 AC4) — không tra cứu động.</summary>
    public decimal TaktTimeSeconds { get; set; }

    /// <summary>Snapshot ProductionPlan.OperatorNames tại thời điểm scan (US-10 AC1/AC5) — KHÔNG phải định danh cá nhân theo lượt scan, xem mục 8.2 SRS.</summary>
    public string OperatorNames { get; set; } = string.Empty;

    public DateTime ScannedAtUtc { get; set; }

    public ScanResult Result { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>US-18 (thay đổi 18/08/2026): Id tài khoản đã đăng nhập xác nhận Scan NG — <c>null</c> khi <see cref="Result"/> != Ng, hoặc bản ghi Ng cũ trước thay đổi này (không backfill).</summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>Tên đăng nhập của <see cref="ConfirmedByUserId"/> — cùng điều kiện null như trên.</summary>
    public string? ConfirmedByUserName { get; set; }

    // --- US-21 AC10/AC11 (18/08/2026, vòng 3): trạng thái rework, suy luận động qua ReworkStatusCalculator ---
    // (US-19) — CHỈ populate khi Result == Ng, null cho mọi trường hợp khác (Ok/DuplicateTag/PreviousStageNotPassed/
    // WaitingReworkUnlock). Quyết định kỹ thuật: mở rộng NGAY DTO dùng chung này (thay vì tạo DTO/API riêng cho
    // US-21) để AC7 vẫn tái dùng ĐÚNG 1 endpoint GET api/v1/scans/history (không tạo API mới) — chi phí tính thêm
    // chỉ phát sinh khi trang kết quả có lượt Ng (xem ScanService.GetHistoryAsync), không ảnh hưởng path Ok thuần
    // túy của US-10.

    /// <summary>US-21 AC10 — null khi <see cref="Result"/> != Ng. Xem <c>ReworkStatusCalculator</c>.</summary>
    public ReworkStatus? ReworkStatus { get; set; }

    /// <summary>
    /// US-21 AC10 "lần N" — chỉ có giá trị khi <see cref="ReworkStatus"/> = <c>ReworkStatus.StillNg</c>, null
    /// trong mọi trường hợp khác.
    /// </summary>
    public int? ReworkStillNgOccurrence { get; set; }

    /// <summary>
    /// US-21 AC11 — "Người sửa hàng" = <c>ReworkUnlock.UnlockedByUserName</c> của lần mở khóa gắn với lượt NG này
    /// (xem <c>ReworkStatusCalculator</c>) — có giá trị khi <see cref="ReworkStatus"/> khác <c>ReworkStatus.NotUnlocked</c>
    /// và khác null; null trong 2 trường hợp còn lại.
    /// </summary>
    public string? ReworkUnlockedByUserName { get; set; }

    /// <summary>US-21 AC11 — thời điểm mở khóa (UTC) tương ứng <see cref="ReworkUnlockedByUserName"/>.</summary>
    public DateTime? ReworkUnlockedAtUtc { get; set; }

    /// <summary>US-21 AC11 — ghi chú của Tổ trưởng khi mở khóa (nếu có), tương ứng <see cref="ReworkUnlockedByUserName"/>.</summary>
    public string? ReworkUnlockNote { get; set; }
}
