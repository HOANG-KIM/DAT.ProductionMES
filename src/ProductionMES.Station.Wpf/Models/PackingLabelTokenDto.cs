namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>PackingLabelTokenDto</c> phía backend (US-24/AC10) — 1 token hợp lệ dùng trong file mẫu tem, hiển thị tham chiếu trên UI.</summary>
public class PackingLabelTokenDto
{
    public string Token { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
