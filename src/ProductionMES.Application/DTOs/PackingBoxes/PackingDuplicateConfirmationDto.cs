namespace ProductionMES.Application.DTOs.PackingBoxes;

/// <summary>Kết quả 1 thao tác xác nhận đã biết tình huống tem trùng tại "Đóng thùng" (US-25 AC8) trả về cho client.</summary>
public class PackingDuplicateConfirmationDto
{
    public int Id { get; set; }

    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    public int ScanId { get; set; }

    public int ConfirmedByUserId { get; set; }

    public string ConfirmedByUserName { get; set; } = string.Empty;

    public DateTime ConfirmedAtUtc { get; set; }

    public string? Note { get; set; }
}
