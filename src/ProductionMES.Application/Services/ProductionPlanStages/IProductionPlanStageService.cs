using ProductionMES.Application.DTOs.ProductionPlanStages;

namespace ProductionMES.Application.Services.ProductionPlanStages;

/// <summary>
/// Service cấu hình công đoạn áp dụng cho từng kế hoạch sản xuất, kèm trình tự (US-03/FR-03) VÀ vòng đời trạng
/// thái riêng của từng cặp (Kế hoạch, Công đoạn) (US-05a/FR-05a).
/// </summary>
public interface IProductionPlanStageService
{
    /// <summary>Thêm 1 công đoạn từ danh mục master vào kế hoạch, mặc định trạng thái Draft (AC1 US-03).</summary>
    Task<ProductionPlanStageDto> AddAsync(int productionPlanId, AddStageToProductionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gỡ 1 công đoạn khỏi kế hoạch, tự động điều chỉnh lại trình tự các công đoạn còn lại (AC2 US-03).</summary>
    Task RemoveAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sắp xếp lại toàn bộ trình tự công đoạn của kế hoạch (AC3 US-03). Từ chối nếu trùng số thứ tự (AC4) hoặc
    /// cấu hình dẫn tới vòng lặp (AC5).
    /// </summary>
    Task<IReadOnlyList<ProductionPlanStageDto>> ReorderAsync(int productionPlanId, ReorderProductionPlanStageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách công đoạn (kèm trình tự, trạng thái và tiến độ "đã chạy/còn lại" tính động) đã cấu hình cho
    /// 1 kế hoạch, sắp theo SequenceNumber.
    /// </summary>
    Task<IReadOnlyList<ProductionPlanStageDto>> GetByProductionPlanAsync(int productionPlanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Áp dụng kế hoạch cho 1 (Line, Công đoạn), chuyển Draft/Paused → Running (US-05a AC1). Từ chối nếu cặp
    /// (Line, Công đoạn) đó đang có 1 kế hoạch KHÁC ở Running — không ràng buộc theo cả Line (AC2), vì các công
    /// đoạn khác nhau của cùng 1 Line được phép chạy kế hoạch khác nhau song song. Nếu cặp này đang Completed/
    /// Cancelled, từ chối (AC7) — không tự "Áp dụng" lại được như Paused.
    /// </summary>
    Task<ProductionPlanStageDto> ApplyAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default);

    /// <summary>Tạm dừng — Running → Paused, giữ nguyên tiến độ vì tính động từ lịch sử scan (US-05a AC3).</summary>
    Task<ProductionPlanStageDto> PauseAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đóng kế hoạch tại công đoạn này (đóng sớm thủ công) → Cancelled (US-05a AC6). Yêu cầu
    /// <see cref="CloseProductionPlanStageRequest.Confirm"/> = true nếu số lượng thực tế ("đã chạy") còn thấp
    /// hơn số lượng kế hoạch.
    /// </summary>
    Task<ProductionPlanStageDto> CloseAsync(int productionPlanId, int stageId, CloseProductionPlanStageRequest request, CancellationToken cancellationToken = default);
}
