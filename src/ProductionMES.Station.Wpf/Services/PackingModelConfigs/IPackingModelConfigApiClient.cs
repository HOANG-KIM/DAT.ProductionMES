using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.PackingModelConfigs;

/// <summary>
/// Gọi <c>api/v1/packing-model-configs</c> (US-24) — dùng chung đúng 1 API với web-admin (AC6), Bearer Tổ trưởng
/// nâng quyền tại trạm (cùng cơ chế elevate <c>SupervisorAuthHandler</c>/<c>ISupervisorSessionService</c> của
/// <c>PlanSettingsPage</c>/<c>LineStageSequencePage</c>).
/// </summary>
public interface IPackingModelConfigApiClient
{
    /// <summary>Toàn bộ cấu hình (AC3).</summary>
    Task<IReadOnlyList<PackingModelConfigDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Gợi ý autocomplete Model đã có cấu hình (AC9).</summary>
    Task<IReadOnlyList<string>> SuggestModelsAsync(string? search, CancellationToken cancellationToken = default);

    /// <summary>Tạo mới cấu hình cho 1 Model (AC1).</summary>
    Task<PackingModelConfigDto> CreateAsync(CreatePackingModelConfigRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sửa cấu hình đã có (AC2).</summary>
    Task<PackingModelConfigDto> UpdateAsync(int id, UpdatePackingModelConfigRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải lên (thay thế) file mẫu tem in từ đường dẫn file cục bộ (AC4). AC10: KHÔNG chặn lưu — kết quả trả về
    /// luôn kèm <see cref="UploadPackingTemplateResultDto.Warnings"/> (có thể rỗng) khi phát hiện token không hợp lệ.
    /// </summary>
    Task<UploadPackingTemplateResultDto> UploadTemplateAsync(int id, string filePath, CancellationToken cancellationToken = default);

    /// <summary>Tải xuống file mẫu tem đang cấu hình, ghi ra đường dẫn file cục bộ <paramref name="destinationFilePath"/> (AC5).</summary>
    Task DownloadTemplateAsync(int id, string destinationFilePath, CancellationToken cancellationToken = default);

    /// <summary>AC10 — danh sách 9 token hợp lệ dùng trong file mẫu tem, để UI hiển thị tham chiếu khi thiết kế file.</summary>
    Task<IReadOnlyList<PackingLabelTokenDto>> GetTemplateTokensAsync(CancellationToken cancellationToken = default);
}
