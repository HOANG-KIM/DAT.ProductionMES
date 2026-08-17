using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.AndonBoard;

/// <summary>Gọi <c>api/v1/andon-board</c> (US-09) — xác thực bằng API Key theo trạm (ADR-005, scheme <c>StationApiKey</c>), cùng cách <c>IScanApiClient</c> đang dùng.</summary>
public interface IAndonBoardApiClient
{
    /// <summary>Lấy dữ liệu bảng PLAN/ACTUAL/BALANCE theo mốc giờ cho đúng trạm đã xác thực.</summary>
    Task<AndonBoardDto> GetAsync(CancellationToken cancellationToken = default);
}
