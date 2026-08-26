using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Request xác nhận LƯU 1 lượt scan bị hệ thống TỰ ĐỘNG từ chối (US-27 AC5/AC6, `POST api/v1/scans/reject-confirmations`)
/// — client (Station.Wpf) gửi lại NGUYÊN VẸN các field đã nhận ở <see cref="ScanResultDto"/> lúc scan (Bước 1,
/// <c>ScansController.Create</c> đã KHÔNG lưu khi <c>Result != Ok</c>, xem AC3). Server CHỈ validate
/// <see cref="Result"/> khác <see cref="ScanResult.Ok"/>/<see cref="ScanResult.Ng"/> rồi lưu kèm
/// <c>ConfirmedByUserId</c>/<c>ConfirmedByUserName</c> lấy từ claim JWT — KHÔNG chạy lại 3 bước kiểm tra FR-08/
/// US-19 (bản ghi phải phản ánh đúng thời điểm scan GỐC, không phải lúc Tổ trưởng đăng nhập xong — nhất quán
/// nguyên tắc snapshot đã có ở FR-10/US-10).
/// </summary>
public class ConfirmRejectedScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int LineId { get; set; }

    public int WorkStationId { get; set; }

    public int ProductionPlanId { get; set; }

    /// <summary>Snapshot ProductionPlan.Customer tại thời điểm scan GỐC (US-10) — không tra cứu động.</summary>
    public string Customer { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Model tại thời điểm scan GỐC (US-10) — không tra cứu động.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Lot tại thời điểm scan GỐC (US-10) — không tra cứu động.</summary>
    public string Lot { get; set; } = string.Empty;

    /// <summary>Snapshot ProductionPlan.Revision tại thời điểm scan GỐC (US-10) — không tra cứu động.</summary>
    public string? Revision { get; set; }

    /// <summary>Snapshot ProductionPlan.PlannedQuantity tại thời điểm scan GỐC (US-10) — không tra cứu động.</summary>
    public int PlannedQuantity { get; set; }

    /// <summary>Snapshot ProductionPlan.TaktTimeSeconds tại thời điểm scan GỐC (US-10) — không tra cứu động.</summary>
    public decimal TaktTimeSeconds { get; set; }

    /// <summary>Snapshot ProductionPlan.OperatorNames tại thời điểm scan GỐC (US-10 AC1/AC5) — không tra cứu động.</summary>
    public string OperatorNames { get; set; } = string.Empty;

    /// <summary>Thời điểm scan GỐC (KHÔNG phải lúc Tổ trưởng đăng nhập xác nhận) — xem remarks lớp.</summary>
    public DateTime ScannedAtUtc { get; set; }

    /// <summary>
    /// Kết quả từ chối đã hiển thị ở banner Lưu/Thoát (AC3) — BẮT BUỘC khác <see cref="ScanResult.Ok"/> (luồng OK
    /// lưu ngay, không qua endpoint này) và khác <see cref="ScanResult.Ng"/> (luồng US-18, endpoint riêng
    /// <c>ScanNgController</c>), validate ở <c>ConfirmRejectedScanRequestValidator</c>.
    /// </summary>
    public ScanResult Result { get; set; }

    /// <summary>Lý do bị từ chối đã hiển thị ở banner (AC3) — lưu lại y nguyên, không tính toán lại.</summary>
    public string? RejectionReason { get; set; }
}
