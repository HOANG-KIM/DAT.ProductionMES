namespace ProductionMES.Application.DTOs.PackingBoxes;

/// <summary>
/// Request xác nhận đã biết tình huống tem trùng tại "Đóng thùng" (US-25 AC8, <c>POST api/v1/packing-boxes/duplicate-confirmations</c>)
/// — cần Bearer Tổ trưởng + permission <c>PackingBox.ConfirmDuplicate</c>, cùng lý do/cách xác định trạm với
/// <see cref="UpdateCurrentBoxNoRequest"/>. Người xác nhận lấy từ claim Bearer token ở Controller.
/// </summary>
public class ConfirmPackingDuplicateRequest
{
    public int WorkStationId { get; set; }

    public string TagCode { get; set; } = string.Empty;

    public string? Note { get; set; }
}
