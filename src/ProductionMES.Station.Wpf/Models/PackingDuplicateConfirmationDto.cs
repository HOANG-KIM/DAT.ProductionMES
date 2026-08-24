namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>PackingDuplicateConfirmationDto</c> phía backend (US-25 AC8).</summary>
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
