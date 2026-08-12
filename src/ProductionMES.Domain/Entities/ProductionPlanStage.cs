namespace ProductionMES.Domain.Entities;

/// <summary>
/// Cấu hình công đoạn áp dụng cho 1 kế hoạch sản xuất, kèm trình tự (FR-03/US-03).
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
/// </remarks>
public class ProductionPlanStage
{
    public int Id { get; set; }

    /// <summary>Kế hoạch sản xuất áp dụng công đoạn này.</summary>
    public int ProductionPlanId { get; set; }

    /// <summary>Công đoạn (từ danh mục master) được áp dụng vào kế hoạch.</summary>
    public int StageId { get; set; }

    /// <summary>Số thứ tự công đoạn trong kế hoạch — duy nhất trong phạm vi 1 kế hoạch (AC4).</summary>
    public int SequenceNumber { get; set; }
}
