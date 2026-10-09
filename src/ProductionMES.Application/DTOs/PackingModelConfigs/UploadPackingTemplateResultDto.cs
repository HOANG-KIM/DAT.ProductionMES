namespace ProductionMES.Application.DTOs.PackingModelConfigs;

/// <summary>
/// Kết quả tải lên file mẫu tem (AC4) — luôn lưu file thành công nếu qua được validate định dạng/kích thước
/// (AC10 KHÔNG chặn cứng việc lưu), kèm theo danh sách cảnh báo token không hợp lệ (nếu có) để Admin/Tổ trưởng
/// biết ngay lúc upload.
/// </summary>
public class UploadPackingTemplateResultDto
{
    public PackingModelConfigDto Config { get; set; } = null!;

    /// <summary>Rỗng nếu file có ít nhất 1 token hợp lệ và không có chuỗi <c>{{...}}</c> nào lạ (AC10).</summary>
    public IReadOnlyList<PackingTemplateWarningDto> Warnings { get; set; } = Array.Empty<PackingTemplateWarningDto>();
}
