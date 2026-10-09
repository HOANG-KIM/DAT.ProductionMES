namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>PackingTemplateWarningDto</c> phía backend (US-24/AC10) — 1 dòng cảnh báo token không hợp lệ khi quét file mẫu tem vừa tải lên.</summary>
public class PackingTemplateWarningDto
{
    public string Message { get; set; } = string.Empty;

    /// <summary>Địa chỉ ô dạng "TênSheet!B2" nếu xác định được vị trí cụ thể — <c>null</c> cho cảnh báo tổng quát.</summary>
    public string? CellReference { get; set; }
}
