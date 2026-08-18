namespace ProductionMES.Application.DTOs.ReworkUnlocks;

/// <summary>
/// Request "Mở khóa rework" (US-19 AC2, <c>POST api/v1/scans/rework-unlock</c>) — cùng cách xác định trạm/công
/// đoạn với <c>CreateNgScanRequest</c> (US-18): <see cref="WorkStationId"/> do Station.Wpf tự khai từ
/// <c>StationOptions.WorkStationId</c> cục bộ (endpoint dùng Bearer Tổ trưởng, không còn claim trạm để đối chiếu
/// như <c>StationApiKey</c>) — Tổ trưởng đứng tại đúng trạm/công đoạn đang có tem bị khóa để mở khóa, không cần
/// chọn Công đoạn riêng trên UI. Người mở khóa (UnlockedByUserId/UnlockedByUserName) lấy từ claim Bearer token ở
/// Controller, KHÔNG nằm trong DTO này (tránh giả mạo, cùng cách <c>ScanNgController</c> đã làm).
/// </summary>
public class ReworkUnlockRequest
{
    public string TagCode { get; set; } = string.Empty;

    public int WorkStationId { get; set; }

    /// <summary>AC2: ghi chú tùy chọn (vd mô tả cách đã sửa lỗi) — có thể để trống.</summary>
    public string? Note { get; set; }
}
