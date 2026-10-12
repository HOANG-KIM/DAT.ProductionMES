namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Request kiểm tra tem KHÔNG lưu (US-12 AC3/AC4, <c>POST api/v1/scans/check</c>). <see cref="WorkStationId"/> là field
/// cấp cao để <c>StationApiKeyAuthenticationHandler</c> đối chiếu chống giả danh (ADR-005 AC6) — Controller/Service dùng
/// trạm lấy từ claim API key, không dùng giá trị này.
/// </summary>
public class CheckScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }
}
