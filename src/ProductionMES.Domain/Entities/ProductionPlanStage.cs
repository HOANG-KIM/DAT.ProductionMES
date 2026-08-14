using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>
/// Cấu hình công đoạn áp dụng cho 1 kế hoạch sản xuất, kèm trình tự (FR-03/US-03) VÀ trạng thái vòng đời riêng
/// của cặp (Kế hoạch, Công đoạn) đó (FR-05a/US-05a — xem <see cref="PlanStatus"/>).
/// </summary>
/// <remarks>
/// Quyết định thiết kế (US-03): dùng mô hình danh sách tuyến tính đơn giản — mỗi bản ghi là 1 công đoạn thuộc
/// 1 kế hoạch, kèm số thứ tự <see cref="SequenceNumber"/> duy nhất trong phạm vi kế hoạch đó. "Công đoạn liền trước"
/// (khái niệm dùng cho FR-08) được <b>suy ra</b> từ bản ghi có <c>SequenceNumber = SequenceNumber hiện tại - 1</c> trong cùng
/// kế hoạch, không lưu con trỏ/FK riêng trỏ tới "công đoạn liền trước".
///
/// Lý do mô hình này vẫn thỏa AC5 (US-03 — "từ chối khi tạo vòng lặp"): với ràng buộc bổ sung "1 công đoạn
/// không xuất hiện quá 1 lần trong cùng 1 kế hoạch" (unique theo ProductionPlanId + StageId, xem
/// <c>ProductionPlanStageConfiguration</c>), quan hệ "liền trước" suy ra từ SequenceNumber-1 luôn tạo thành 1 chuỗi tuyến
/// tính đơn giản (mỗi công đoạn có tối đa 1 công đoạn liền trước, tối đa 1 công đoạn liền sau) — về mặt cấu
/// trúc không thể hình thành vòng lặp (A liền trước B, B liền trước A) vì điều đó đòi hỏi A và B cùng lúc vừa
/// đứng trước vừa đứng sau nhau, mâu thuẫn với thứ tự tuyến tính duy nhất theo SequenceNumber. Do đó AC5 được đảm bảo
/// ngay từ ràng buộc dữ liệu, không cần thuật toán phát hiện chu trình (cycle detection) riêng.
///
/// Quyết định thiết kế (US-05a, 14/08/2026): entity này đại diện ĐÚNG cặp (Kế hoạch, Công đoạn) mô tả trong
/// FR-05a, nên trạng thái vòng đời <see cref="PlanStatus"/> được đặt trực tiếp ở đây thay vì tạo 1 entity join
/// mới hay đặt cờ ở <see cref="ProductionPlan"/> (xem remarks tại entity đó để biết lý do đầy đủ).
/// </remarks>
public class ProductionPlanStage
{
    public int Id { get; set; }

    /// <summary>Kế hoạch sản xuất áp dụng công đoạn này.</summary>
    public int ProductionPlanId { get; set; }

    /// <summary>Công đoạn (từ danh mục master) được áp dụng vào kế hoạch.</summary>
    public int StageId { get; set; }

    /// <summary>
    /// Line của kế hoạch cha — sao chép (denormalize) từ <see cref="ProductionPlan.LineId"/> tại thời điểm thêm
    /// công đoạn vào kế hoạch (US-03 AddAsync). LineId của 1 ProductionPlan không đổi trong vòng đời (không có
    /// API nào cho sửa LineId), nên sao chép an toàn. Mục đích: cho phép truy vấn hiệu quả ràng buộc "tối đa 1
    /// kế hoạch Running theo cặp (Line, Công đoạn)" (FR-05/mục 6 quy tắc 12, US-05a AC1/AC2) mà không phải join
    /// sang bảng ProductionPlan mỗi lần kiểm tra (đặc biệt quan trọng ở ScanService — chạy trên mọi lượt scan).
    /// </summary>
    public int LineId { get; set; }

    /// <summary>Số thứ tự công đoạn trong kế hoạch — duy nhất trong phạm vi 1 kế hoạch (AC4).</summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Vòng đời trạng thái CỦA CẶP (Kế hoạch, Công đoạn) này (FR-05a/US-05a): <c>Draft</c> (mặc định khi vừa
    /// thêm vào kế hoạch, chưa từng "Áp dụng") → <c>Running</c> ⇄ <c>Paused</c> → <c>Completed</c>/<c>Cancelled</c>.
    /// </summary>
    public PlanStatus PlanStatus { get; set; } = PlanStatus.Draft;
}
