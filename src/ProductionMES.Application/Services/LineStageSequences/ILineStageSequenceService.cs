using ProductionMES.Application.DTOs.LineStageSequences;

namespace ProductionMES.Application.Services.LineStageSequences;

/// <summary>
/// Service cấu hình trình tự công đoạn của 1 Line sản xuất (US-03/FR-03) — thiết lập 1 lần, dùng chung cho mọi
/// kế hoạch chạy trên Line đó.
/// </summary>
public interface ILineStageSequenceService
{
    /// <summary>Thêm 1 công đoạn từ danh mục master vào trình tự của Line, mặc định vào cuối danh sách (AC1).</summary>
    Task<LineStageSequenceDto> AddAsync(int lineId, AddStageToLineRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gỡ 1 công đoạn khỏi trình tự của Line, tự động điều chỉnh lại trình tự các công đoạn còn lại (AC2). Từ
    /// chối (ném <c>BusinessRuleException</c>) nếu đang có bất kỳ kế hoạch nào của Line đó ở trạng thái
    /// <c>Running</c>/<c>Paused</c> tại đúng công đoạn sắp gỡ.
    /// </summary>
    Task RemoveAsync(int lineId, int stageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sắp xếp lại toàn bộ trình tự công đoạn của Line (AC3). Từ chối nếu trùng số thứ tự (AC4) hoặc cấu hình
    /// dẫn tới vòng lặp (AC5).
    /// </summary>
    Task<IReadOnlyList<LineStageSequenceDto>> ReorderAsync(int lineId, ReorderLineStageSequenceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lấy danh sách công đoạn (kèm trình tự) đã cấu hình cho 1 Line, sắp theo SequenceNumber.</summary>
    Task<IReadOnlyList<LineStageSequenceDto>> GetByLineAsync(int lineId, CancellationToken cancellationToken = default);
}
