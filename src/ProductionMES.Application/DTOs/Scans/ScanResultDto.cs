using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Kết quả 1 lượt scan trả về cho client (US-07/US-08) — đủ dữ liệu để Station.Wpf (tương lai) hiển thị popup
/// OK/lỗi đúng theo FR-07 (mã tem, kết quả để chọn màu/âm thanh, lý do bị từ chối rõ ràng).
/// </summary>
public class ScanResultDto
{
    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int LineId { get; set; }

    public int WorkStationId { get; set; }

    public int ProductionPlanId { get; set; }

    /// <summary>Snapshot ProductionPlan.Customer tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Model tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Lot tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Revision tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public string? Revision { get; set; }

    /// <summary>Snapshot ProductionPlan.PlannedQuantity tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>Snapshot ProductionPlan.TaktTimeSeconds tại thời điểm scan (US-10) — không tra cứu động.</summary>
    public decimal TaktTimeSeconds { get; set; }

    /// <summary>Snapshot ProductionPlan.OperatorNames tại thời điểm scan (US-10 AC1/AC5) — không tra cứu động.</summary>
    public string OperatorNames { get; set; } = string.Empty;

    public DateTime ScannedAtUtc { get; set; }

    public ScanResult Result { get; set; }

    /// <summary>Lý do bị từ chối (vd "Chưa qua công đoạn: Lắp ráp") — <c>null</c> khi <see cref="Result"/> = Ok.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>US-18 (thay đổi 18/08/2026): Id tài khoản đã đăng nhập xác nhận Scan NG — <c>null</c> khi <see cref="Result"/> != Ng, hoặc bản ghi Ng cũ trước thay đổi này (không backfill).</summary>
    public int? ConfirmedByUserId { get; set; }

    /// <summary>Tên đăng nhập của <see cref="ConfirmedByUserId"/> — cùng điều kiện null như trên.</summary>
    public string? ConfirmedByUserName { get; set; }

    /// <summary>
    /// US-25: true khi <see cref="StageId"/> là công đoạn "Đóng thùng" (<c>Stage.IsPackingStage</c>) — các field
    /// <c>Packing*</c> bên dưới chỉ có giá trị (khác mặc định) khi cờ này true VÀ <see cref="Result"/> = Ok (AC2).
    /// Station.Wpf dùng cờ này để quyết định có hiển thị/cập nhật bộ đếm thùng hay không, KHÔNG dựa vào cấu hình
    /// cục bộ trùng lặp — nguồn thật vẫn là server (AC1/AC14: mọi hành vi đặc thù chỉ áp dụng đúng công đoạn này).
    /// </summary>
    public bool IsPackingStage { get; set; }

    /// <summary>Số thùng hiện tại SAU lượt scan này (AC2/AC4/AC9) — chỉ có giá trị khi <see cref="IsPackingStage"/> = true và <see cref="Result"/> = Ok.</summary>
    public int? PackingBoxNo { get; set; }

    /// <summary>Số lượng đã quét trong thùng hiện tại SAU lượt scan này (AC2/AC9) — 0 nếu thùng vừa hoàn tất và mở thùng kế tiếp (AC4).</summary>
    public int? PackingScannedQuantity { get; set; }

    /// <summary>Số lượng mục tiêu (snapshot Quy cách đóng gói) của thùng hiện tại SAU lượt scan này (AC2/AC9/AC12).</summary>
    public int? PackingTargetQuantity { get; set; }

    /// <summary>true nếu ĐÚNG lượt scan này vừa làm đủ số lượng, hoàn tất 1 thùng (AC4) — Station.Wpf dùng để quyết định có tự động in tem hay không.</summary>
    public bool PackingBoxCompleted { get; set; }

    /// <summary>Id thùng VỪA hoàn tất (khi <see cref="PackingBoxCompleted"/> = true) — dùng để gọi endpoint tải tem in (AC4/AC13). Null nếu <see cref="PackingBoxCompleted"/> = false.</summary>
    public int? PackingCompletedBoxId { get; set; }
}
