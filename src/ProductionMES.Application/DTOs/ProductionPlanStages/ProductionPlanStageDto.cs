using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.ProductionPlanStages;

/// <summary>DTO trả về cho client, đại diện 1 công đoạn trong trình tự của 1 kế hoạch sản xuất (US-03, US-05a).</summary>
public class ProductionPlanStageDto
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int SequenceNumber { get; set; }

    /// <summary>
    /// Id công đoạn liền trước trong cùng kế hoạch (suy ra từ SequenceNumber - 1) — null nếu đây là công đoạn đầu tiên
    /// (FR-03/FR-08).
    /// </summary>
    public int? PreviousStageId { get; set; }

    /// <summary>Trạng thái vòng đời của cặp (Kế hoạch, Công đoạn) này (FR-05a/US-05a).</summary>
    public PlanStatus PlanStatus { get; set; }

    /// <summary>
    /// "Đã chạy" — tổng số lượt scan kết quả OK tại đúng cặp (Kế hoạch, Công đoạn) này, tính động từ lịch sử
    /// scan (US-05a AC4), KHÔNG đọc từ 1 cột số liệu lưu sẵn.
    /// </summary>
    public int RunCount { get; set; }

    /// <summary>"Còn lại" = Số lượng kế hoạch − RunCount, tính động (US-05a AC4), không nhỏ hơn 0.</summary>
    public int RemainingCount { get; set; }
}
