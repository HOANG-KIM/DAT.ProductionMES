namespace ProductionMES.Application.DTOs.PackingBoxes;

/// <summary>Request nhập số thùng bắt đầu (US-25 AC5, <c>POST api/v1/packing-boxes/starting-box-no</c>) — scheme StationApiKey, không cần đăng nhập Supervisor.</summary>
public class SetStartingBoxNoRequest
{
    public int StartingBoxNo { get; set; }
}
