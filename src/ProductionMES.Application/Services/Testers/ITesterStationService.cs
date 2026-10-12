using ProductionMES.Application.DTOs.Testers;

namespace ProductionMES.Application.Services.Testers;

/// <summary>Công tắc máy thông điện theo trạm + log sự kiện máy (US-11, US-14 AC12).</summary>
public interface ITesterStationService
{
    /// <summary>Đọc trạng thái máy thông điện (<c>UseArduino</c> + công tắc) của trạm — AC10.</summary>
    Task<TesterStationStateDto> GetStateAsync(int workStationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đổi công tắc "Đang sử dụng máy thông điện" (AC4): chỉ trạm có <c>UseArduino = true</c>, bắt buộc có lý do; ghi
    /// 1 bản ghi <c>TesterModeChangeLog</c> kèm người duyệt. Nếu công tắc đã ở đúng giá trị mong muốn thì không đổi và
    /// không ghi log (idempotent).
    /// </summary>
    Task<TesterStationStateDto> SetEnabledAsync(
        int workStationId, bool enabled, string reason, int approvedByUserId, string approvedByUserName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi 1 sự kiện máy thông điện (US-14 AC12) cho trạm <paramref name="workStationId"/>. Thời điểm lấy theo giờ server.
    /// </summary>
    Task RecordEventAsync(int workStationId, RecordTesterEventRequest request, CancellationToken cancellationToken = default);
}
