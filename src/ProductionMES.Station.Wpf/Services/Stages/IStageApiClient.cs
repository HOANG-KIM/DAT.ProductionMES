using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Stages;

/// <summary>Gọi <c>api/v1/stages</c> (US-02, danh mục Công đoạn master) — yêu cầu đã đăng nhập Supervisor có quyền <c>Stage.View</c>.</summary>
public interface IStageApiClient
{
    Task<IReadOnlyList<StageDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
