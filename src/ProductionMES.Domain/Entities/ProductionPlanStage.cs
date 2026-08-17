using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// Trạng thái vòng đời (FR-05a/US-05a) của 1 cặp (Kế hoạch sản xuất, Công đoạn) — <see cref="PlanStatus"/>.
/// </summary>
/// <remarks>
/// Quyết định thiết kế lại (US-03, 17/08/2026): entity này KHÔNG còn mang trình tự công đoạn (đã bỏ property
/// <c>SequenceNumber</c> cũ). Trình tự công đoạn (Stage nào, thứ tự nào) là cấu hình của <b>Line</b>, thiết lập 1
/// lần và dùng chung cho mọi kế hoạch chạy trên Line đó — xem <see cref="LineStageSequence"/>. Bản ghi
/// <c>ProductionPlanStage</c> giờ đây chỉ đại diện đúng vòng đời <see cref="PlanStatus"/> của 1 cặp (Kế hoạch,
/// Công đoạn), được tạo "lazy" (get-or-create) khi lần đầu cần đọc/thao tác tới đúng cặp đó (xem
/// <c>ProductionPlanStageService</c>), không còn được tạo qua thao tác "gắn công đoạn vào kế hoạch" như thiết kế
/// cũ (đã bỏ hẳn AddAsync/RemoveAsync/ReorderAsync ở service này, chuyển sang <c>LineStageSequenceService</c>).
///
/// Quyết định thiết kế (US-05a, 14/08/2026, vẫn giữ nguyên): entity này đại diện đúng cặp (Kế hoạch, Công đoạn)
/// mô tả trong FR-05a, nên trạng thái vòng đời <see cref="PlanStatus"/> được đặt trực tiếp ở đây thay vì tạo 1
/// entity join mới hay đặt cờ ở <see cref="ProductionPlan"/> (xem remarks tại entity đó để biết lý do đầy đủ).
/// </remarks>
public class ProductionPlanStage
{
    public int Id { get; set; }

    /// <summary>Kế hoạch sản xuất áp dụng công đoạn này.</summary>
    public int ProductionPlanId { get; set; }

    /// <summary>Công đoạn (từ danh mục master) được áp dụng vào kế hoạch.</summary>
    public int StageId { get; set; }

    /// <summary>
    /// Line của kế hoạch cha — sao chép (denormalize) từ <see cref="ProductionPlan.LineId"/> tại thời điểm tạo
    /// bản ghi (lazy get-or-create, xem remarks). LineId của 1 ProductionPlan không đổi trong vòng đời (không có
    /// API nào cho sửa LineId), nên sao chép an toàn. Mục đích: cho phép truy vấn hiệu quả ràng buộc "tối đa 1
    /// kế hoạch Running theo cặp (Line, Công đoạn)" (FR-05/mục 6 quy tắc 12, US-05a AC1/AC2) mà không phải join
    /// sang bảng ProductionPlan mỗi lần kiểm tra (đặc biệt quan trọng ở ScanService — chạy trên mọi lượt scan).
    /// </summary>
    public int LineId { get; set; }

    /// <summary>
    /// Vòng đời trạng thái CỦA CẶP (Kế hoạch, Công đoạn) này (FR-05a/US-05a): <c>Draft</c> (mặc định khi vừa
    /// được tạo, chưa từng "Áp dụng") → <c>Running</c> ⇄ <c>Paused</c> → <c>Completed</c>/<c>Cancelled</c>.
    /// </summary>
    public PlanStatus PlanStatus { get; set; } = PlanStatus.Draft;
}
