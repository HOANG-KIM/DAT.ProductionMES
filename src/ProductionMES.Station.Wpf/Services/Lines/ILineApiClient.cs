using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Lines;

/// <summary>Gọi <c>api/v1/lines</c> (US-01, danh mục Line sản xuất) — yêu cầu đã đăng nhập Supervisor có quyền <c>Line.View</c>.</summary>
public interface ILineApiClient
{
    Task<IReadOnlyList<LineDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
