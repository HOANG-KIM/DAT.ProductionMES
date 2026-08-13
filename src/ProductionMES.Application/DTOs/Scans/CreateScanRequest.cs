namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Request tạo 1 lượt scan (US-07/US-08, `POST api/v1/scans`).
/// </summary>
/// <remarks>
/// <see cref="WorkStationId"/> vẫn cần có mặt trong body (field JSON cấp cao <c>workStationId</c>, đúng tên đã
/// chốt) để <c>StationApiKeyAuthenticationHandler</c> đọc và đối chiếu chống giả danh trạm (ADR-005 AC6). Tuy
/// nhiên Controller/Service KHÔNG dùng giá trị này để xử lý business logic — nguồn thật là
/// <c>StationApiKeyDefaults.WorkStationIdClaimType</c> lấy từ danh tính trạm đã xác thực.
/// </remarks>
public class CreateScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }
}
