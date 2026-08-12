namespace ProductionMES.Application.DTOs.Lines;

/// <summary>Request cập nhật tên/mô tả 1 Line đã tồn tại (AC2). Đổi trạng thái hoạt động dùng endpoint riêng (AC3).</summary>
public class UpdateLineRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
