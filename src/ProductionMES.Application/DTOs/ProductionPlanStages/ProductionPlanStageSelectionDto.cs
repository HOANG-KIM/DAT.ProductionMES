using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.ProductionPlanStages;

/// <summary>
/// DTO cho màn hình "Chọn kế hoạch" (US-05b) — 1 kế hoạch đã cấu hình áp dụng cho 1 (Line, Công đoạn) cụ thể,
/// gộp cả thông tin gốc của kế hoạch (US-05) lẫn trạng thái/tiến độ tại đúng công đoạn đó (US-05a), để client
/// không phải gọi thêm request phụ lấy <see cref="Application.DTOs.ProductionPlans.ProductionPlanDto"/> riêng.
/// </summary>
public class ProductionPlanStageSelectionDto
{
    public int ProductionPlanId { get; set; }

    public int StageId { get; set; }

    public int LineId { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Lot { get; set; } = string.Empty;

    public string? Revision { get; set; }

    public int PlannedQuantity { get; set; }

    public decimal TaktTimeSeconds { get; set; }

    /// <summary>Sản lượng chuẩn/giờ = 3600 / Takt time (FR-06), tính lúc map, không lưu cột riêng.</summary>
    public decimal StandardQuantityPerHour { get; set; }

    public DateTime StartTime { get; set; }

    public string OperatorNames { get; set; } = string.Empty;

    /// <summary>Trạng thái vòng đời của đúng cặp (Kế hoạch, Công đoạn) này (US-05a).</summary>
    public PlanStatus PlanStatus { get; set; }

    /// <summary>"Đã chạy" tại đúng công đoạn này, tính động từ lịch sử scan OK (US-05a AC4).</summary>
    public int RunCount { get; set; }

    /// <summary>"Còn lại" = PlannedQuantity − RunCount, không nhỏ hơn 0 (US-05a AC4).</summary>
    public int RemainingCount { get; set; }
}
