using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.ProductionPlanStages;

/// <summary>
/// Gọi <c>api/v1/production-plan-stages</c> và <c>api/v1/production-plans/{id}/stages</c> (US-05a/US-05b) — vòng
/// đời trạng thái theo cặp (Kế hoạch, Công đoạn). Trình tự công đoạn (US-03) KHÔNG còn ở đây — xem
/// <c>ILineStageSequenceApiClient</c>.
/// </summary>
public interface IProductionPlanStageApiClient
{
    /// <summary>Màn "Chọn kế hoạch" (US-05b AC2) — danh sách kế hoạch áp dụng cho (Line, Công đoạn).</summary>
    Task<IReadOnlyList<ProductionPlanStageSelectionDto>> GetByLineAndStageAsync(
        int lineId, int stageId, bool includeClosed = false, CancellationToken cancellationToken = default);

    Task<ProductionPlanStageDto> ApplyAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default);

    Task<ProductionPlanStageDto> PauseAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default);

    Task<ProductionPlanStageDto> CloseAsync(int productionPlanId, int stageId, bool confirm, CancellationToken cancellationToken = default);
}
