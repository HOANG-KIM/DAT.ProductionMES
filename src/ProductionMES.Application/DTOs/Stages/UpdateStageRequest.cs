namespace ProductionMES.Application.DTOs.Stages;

/// <summary>Request cập nhật tên/mô tả 1 công đoạn đã tồn tại. Đổi trạng thái hoạt động dùng endpoint riêng (AC3).</summary>
public class UpdateStageRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>US-25: đánh dấu đây là công đoạn "Đóng thùng" đặc thù.</summary>
    public bool IsPackingStage { get; set; }
}
