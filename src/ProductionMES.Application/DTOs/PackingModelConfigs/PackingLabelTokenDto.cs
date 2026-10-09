namespace ProductionMES.Application.DTOs.PackingModelConfigs;

/// <summary>1 token hợp lệ dùng trong file mẫu tem in (template .xlsx) — khớp <see cref="Services.PackingBoxes.PackingLabelTokenInfo"/>, trả cho UI hiển thị tham chiếu khi thiết kế file (US-24/AC10).</summary>
public class PackingLabelTokenDto
{
    public string Token { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
