using ProductionMES.Application.DTOs.StationApiKeys;

namespace ProductionMES.Application.Services.StationApiKeys;

/// <summary>Service quản lý API Key theo trạm (US-04a, ADR-005).</summary>
public interface IStationApiKeyService
{
    /// <summary>Cấp API Key mới cho trạm — từ chối nếu trạm đang có 1 key Active (dùng <see cref="ReissueAsync"/> để xoay vòng) (AC1).</summary>
    Task<IssuedStationApiKeyDto> IssueAsync(int workStationId, CancellationToken cancellationToken = default);

    /// <summary>Thu hồi key Active hiện tại của trạm — từ chối nếu trạm không có key Active nào (AC3).</summary>
    Task RevokeAsync(int workStationId, CancellationToken cancellationToken = default);

    /// <summary>Xoay vòng: thu hồi key Active hiện tại (nếu có) và cấp key mới, giữ lại lịch sử key cũ (AC4).</summary>
    Task<IssuedStationApiKeyDto> ReissueAsync(int workStationId, CancellationToken cancellationToken = default);

    /// <summary>Lấy metadata key mới nhất của trạm (Active hoặc đã Revoked) — không có key nào trả về <c>null</c> (AC2).</summary>
    Task<StationApiKeyDto?> GetCurrentAsync(int workStationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác thực 1 API key thô nhận từ header <c>X-Station-Api-Key</c> (dùng bởi <c>AuthenticationScheme
    /// "StationApiKey"</c>). Trả về <c>WorkStationId</c> đã xác thực nếu key hợp lệ và chưa bị thu hồi (AC5);
    /// nếu <paramref name="expectedWorkStationId"/> có giá trị (đọc từ request body của endpoint gọi vào), còn
    /// đối chiếu phải khớp đúng trạm sở hữu key, từ chối (trả <c>null</c>) nếu sai trạm (AC6/ADR-005).
    /// </summary>
    Task<int?> ValidateAsync(string rawApiKey, int? expectedWorkStationId, CancellationToken cancellationToken = default);
}
