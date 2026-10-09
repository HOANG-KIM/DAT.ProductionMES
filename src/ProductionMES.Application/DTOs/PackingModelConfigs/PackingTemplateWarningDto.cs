namespace ProductionMES.Application.DTOs.PackingModelConfigs;

/// <summary>
/// 1 dòng cảnh báo khi quét token trong file mẫu tem vừa tải lên (US-24/AC10) — KHÔNG chặn việc lưu file, chỉ để
/// Admin/Tổ trưởng biết ngay lúc upload thay vì phát hiện sau khi tem đã in sai ngoài xưởng.
/// </summary>
public class PackingTemplateWarningDto
{
    public string Message { get; set; } = string.Empty;

    /// <summary>Địa chỉ ô dạng "TênSheet!B2" nếu xác định được vị trí cụ thể — <c>null</c> cho cảnh báo tổng quát (vd "không có token nào trong toàn bộ file").</summary>
    public string? CellReference { get; set; }
}
