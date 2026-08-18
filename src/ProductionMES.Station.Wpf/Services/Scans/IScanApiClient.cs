using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Scans;

/// <summary>Gọi <c>api/v1/scans</c> (US-07/US-08/US-18) — xác thực bằng API Key theo trạm (ADR-005, scheme <c>StationApiKey</c>), không cần đăng nhập Supervisor.</summary>
public interface IScanApiClient
{
    /// <summary>
    /// Ghi nhận 1 lượt scan. Luôn trả về <see cref="ScanResultDto"/> (kể cả khi bị từ chối — DuplicateTag/
    /// PreviousStageNotPassed là kết quả nghiệp vụ hợp lệ, không phải lỗi HTTP, xem FR-08/FR-10).
    /// </summary>
    Task<ScanResultDto> CreateAsync(string tagCode, int workStationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// US-18 AC3/AC5 (thay đổi 18/08/2026): ghi nhận 1 lượt Scan NG kèm lý do lỗi bắt buộc (Chế độ Scan NG tại
    /// trạm). KHÁC <see cref="CreateAsync"/>: endpoint <c>POST api/v1/scans/ng</c> nay yêu cầu Bearer token Tổ
    /// trưởng (ADR-005 mục 2) thay vì StationApiKey — <paramref name="supervisorAccessToken"/> là access token
    /// đã đăng nhập RIÊNG cho lượt Scan NG này (<c>ISupervisorAuthService.LoginForNgConfirmationAsync</c>, AC2a).
    /// </summary>
    Task<ScanResultDto> CreateNgAsync(string tagCode, int workStationId, string rejectionReason, string supervisorAccessToken, CancellationToken cancellationToken = default);

    /// <summary>US-18 AC4: gợi ý autocomplete các lý do lỗi đã từng nhập cho đúng công đoạn <paramref name="stageId"/>.</summary>
    Task<IReadOnlyList<string>> GetNgReasonSuggestionsAsync(int stageId, CancellationToken cancellationToken = default);
}
