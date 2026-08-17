using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.LineStageSequences;

/// <summary>Gọi <c>api/v1/lines/{lineId}/stage-sequence</c> (US-03) — cấu hình trình tự công đoạn của 1 Line.</summary>
public interface ILineStageSequenceApiClient
{
    /// <summary>Danh sách công đoạn (kèm trình tự) đã cấu hình cho Line, sắp theo SequenceNumber.</summary>
    Task<IReadOnlyList<LineStageSequenceDto>> GetByLineAsync(int lineId, CancellationToken cancellationToken = default);

    /// <summary>Thêm 1 công đoạn từ danh mục master vào trình tự của Line (AC1).</summary>
    Task<LineStageSequenceDto> AddStageAsync(int lineId, int stageId, int? sequenceNumber, CancellationToken cancellationToken = default);

    /// <summary>Gỡ 1 công đoạn khỏi trình tự của Line (AC2) — server chặn hẳn nếu đang có kế hoạch Running/Paused tại công đoạn đó.</summary>
    Task RemoveStageAsync(int lineId, int stageId, CancellationToken cancellationToken = default);

    /// <summary>Sắp xếp lại toàn bộ trình tự công đoạn của Line (AC3) — từ chối nếu trùng thứ tự (AC4) hoặc tạo vòng lặp (AC5).</summary>
    Task<IReadOnlyList<LineStageSequenceDto>> ReorderAsync(
        int lineId, IReadOnlyList<(int StageId, int SequenceNumber)> items, CancellationToken cancellationToken = default);
}
