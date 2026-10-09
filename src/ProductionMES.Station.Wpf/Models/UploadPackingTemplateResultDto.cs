namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>UploadPackingTemplateResultDto</c> phía backend (US-24/AC10) — kết quả tải lên mẫu tem, kèm cảnh báo token (nếu có, KHÔNG chặn lưu).</summary>
public class UploadPackingTemplateResultDto
{
    public PackingModelConfigDto Config { get; set; } = new();

    public IReadOnlyList<PackingTemplateWarningDto> Warnings { get; set; } = Array.Empty<PackingTemplateWarningDto>();
}
