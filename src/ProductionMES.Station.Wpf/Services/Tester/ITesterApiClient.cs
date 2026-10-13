using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Tester;

/// <summary>
/// Gọi <c>api/v1/tester</c> (US-11, US-14 AC12): đọc trạng thái + ghi event log bằng API Key trạm; đổi công tắc bằng
/// Bearer Tổ trưởng.
/// </summary>
public interface ITesterApiClient
{
    /// <summary>US-11 AC10: trạng thái máy thông điện (UseArduino + công tắc) CỦA CHÍNH trạm này (theo API key).</summary>
    Task<TesterStationStateDto> GetStationStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// US-11 AC4: đổi công tắc kèm lý do. <paramref name="supervisorAccessToken"/> là access token đã đăng nhập RIÊNG cho
    /// thao tác này (permission <c>TesterMode.Change</c>), gắn tường minh vào header như <c>IScanApiClient.CreateNgAsync</c>.
    /// </summary>
    Task<TesterStationStateDto> SetEnabledAsync(bool enabled, string reason, string supervisorAccessToken, CancellationToken cancellationToken = default);

    /// <summary>US-14 AC12: ghi 1 event log sự kiện máy (API Key trạm).</summary>
    Task RecordEventAsync(RecordTesterEventRequest request, CancellationToken cancellationToken = default);
}
