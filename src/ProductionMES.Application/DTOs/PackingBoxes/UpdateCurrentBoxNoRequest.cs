namespace ProductionMES.Application.DTOs.PackingBoxes;

/// <summary>
/// Request sửa số thùng hiện tại (US-25 AC7, <c>POST api/v1/packing-boxes/box-no</c>) — cần Bearer Tổ trưởng +
/// permission <c>PackingBox.Update</c> (KHÔNG dùng scheme StationApiKey, cùng lý do <c>ReworkUnlockRequest</c>/
/// <c>CreateNgScanRequest</c>: <see cref="WorkStationId"/> do Station.Wpf tự khai từ <c>StationOptions</c> cục
/// bộ). Người sửa (UpdatedByUserId/UpdatedByUserName) lấy từ claim Bearer token ở Controller, KHÔNG nằm trong DTO này.
/// </summary>
public class UpdateCurrentBoxNoRequest
{
    public int WorkStationId { get; set; }

    public int NewBoxNo { get; set; }
}
