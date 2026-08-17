namespace ProductionMES.Domain.Entities;

/// <summary>
/// Trình tự công đoạn của 1 Line (FR-03/US-03) — nguồn sự thật DUY NHẤT cho việc Stage nào thuộc Line nào và
/// theo thứ tự ra sao. Cấu hình 1 lần cho Line, dùng chung cho MỌI kế hoạch sản xuất (<see cref="ProductionPlan"/>)
/// chạy trên Line đó — không còn cấu hình riêng theo từng kế hoạch như bản thiết kế cũ (xem remarks tại
/// <see cref="ProductionPlanStage"/> để biết lý do thiết kế lại, chốt 17/08/2026).
/// </summary>
/// <remarks>
/// Mô hình dữ liệu giữ nguyên ý tưởng danh sách tuyến tính đã dùng ở <c>ProductionPlanStage</c> trước đây: mỗi
/// bản ghi là 1 công đoạn thuộc trình tự của 1 Line, kèm số thứ tự <see cref="SequenceNumber"/> duy nhất trong
/// phạm vi Line đó. "Công đoạn liền trước" (FR-08) được suy ra từ bản ghi có
/// <c>SequenceNumber = SequenceNumber hiện tại - 1</c> trong cùng Line, không lưu con trỏ riêng. Ràng buộc "1
/// công đoạn không xuất hiện quá 1 lần trong cùng 1 Line" (unique theo LineId + StageId) đảm bảo cấu trúc tuyến
/// tính này không thể tạo vòng lặp khi suy "liền trước" (AC5 US-03) — lý do đầy đủ xem remarks cũ tại
/// <c>ProductionPlanStage</c> (bản trước khi sửa), áp dụng y hệt ở đây.
/// </remarks>
public class LineStageSequence
{
    public int Id { get; set; }

    /// <summary>Line sở hữu trình tự này.</summary>
    public int LineId { get; set; }

    /// <summary>Công đoạn (từ danh mục master) thuộc trình tự của Line.</summary>
    public int StageId { get; set; }

    /// <summary>Số thứ tự công đoạn trong trình tự của Line — duy nhất trong phạm vi 1 Line (AC4).</summary>
    public int SequenceNumber { get; set; }
}
