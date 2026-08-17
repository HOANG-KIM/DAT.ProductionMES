namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror JSON của <c>CreateScanRequest</c> phía backend (US-07/US-08, request <c>POST api/v1/scans</c>).
/// <see cref="WorkStationId"/> lấy từ <c>StationOptions.WorkStationId</c> (cấu hình cục bộ tại trạm), không phải
/// nguồn xử lý business logic thật (server đọc từ claim của API key đã xác thực) — chỉ dùng để đối chiếu chống
/// giả danh (ADR-005 AC6).
/// </summary>
public class CreateScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }
}
