using ProductionMES.Application.DTOs.ProductionPlanStages;

namespace ProductionMES.Application.Services.ProductionPlanStages;

/// <summary>
/// Service quản lý vòng đời trạng thái riêng của từng cặp (Kế hoạch sản xuất, Công đoạn) (US-05a/FR-05a).
/// Trình tự công đoạn (Stage nào, thứ tự nào) KHÔNG còn thuộc phạm vi service này — xem
/// <c>ILineStageSequenceService</c> (US-03/FR-03, cấu hình của Line, dùng chung mọi kế hoạch).
/// </summary>
public interface IProductionPlanStageService
{
    /// <summary>
    /// Lấy danh sách công đoạn (kèm trình tự — suy từ trình tự cấu hình của Line, trạng thái và tiến độ "đã
    /// chạy/còn lại" tính động) áp dụng cho 1 kế hoạch, sắp theo SequenceNumber. Cơ chế "lazy get-or-create":
    /// mọi Stage trong trình tự của Line mà kế hoạch này chưa có bản ghi vòng đời sẽ được tạo mới (Draft) ngay
    /// trong lần gọi này, đảm bảo luôn trả về ĐẦY ĐỦ mọi Stage trong trình tự Line.
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

    /// <summary>
    /// Màn hình "Chọn kế hoạch" (US-05b AC2): liệt kê MỌI kế hoạch của Line, kèm trạng thái/tiến độ tại đúng
    /// công đoạn <paramref name="stageId"/> (US-03 — mọi kế hoạch trên Line tự động áp dụng mọi Stage trong
    /// trình tự Line, không cần bản ghi vòng đời tồn tại trước; mặc định <c>Draft</c> nếu chưa có). Mặc định ẩn
    /// các cặp đã <c>Completed</c>/<c>Cancelled</c> (US-05a AC7) trừ khi <paramref name="includeClosed"/> = true.
    /// </summary>
    Task<IReadOnlyList<ProductionPlanStageSelectionDto>> GetByLineAndStageAsync(
        int lineId, int stageId, bool includeClosed = false, CancellationToken cancellationToken = default);
}
