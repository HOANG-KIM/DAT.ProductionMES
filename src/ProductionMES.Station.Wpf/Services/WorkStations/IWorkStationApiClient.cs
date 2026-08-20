using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.WorkStations;

/// <summary>Gọi <c>api/v1/work-stations</c> (US-04, danh mục trạm làm việc) — yêu cầu đã đăng nhập Supervisor có quyền <c>WorkStation.View</c>.</summary>
public interface IWorkStationApiClient
{
    Task<IReadOnlyList<WorkStationDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
