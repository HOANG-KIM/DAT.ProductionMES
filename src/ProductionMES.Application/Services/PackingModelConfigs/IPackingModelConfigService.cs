using ProductionMES.Application.DTOs.PackingModelConfigs;

namespace ProductionMES.Application.Services.PackingModelConfigs;

/// <summary>
/// Quản lý cấu hình Quy cách đóng gói theo Model (US-24/FR-24) — dùng chung 1 nguồn duy nhất qua API trung tâm
/// cho cả web-admin (Admin) và Station.Wpf (Tổ trưởng nâng quyền tại trạm), AC6.
/// </summary>
public interface IPackingModelConfigService
{
    /// <summary>Toàn bộ cấu hình (AC3) — danh mục nhỏ, không phân trang (cùng quy ước Line/Stage hiện có).</summary>
    Task<IReadOnlyList<PackingModelConfigDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PackingModelConfigDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tra cứu theo Model — so khớp không phân biệt hoa/thường, tự trim khoảng trắng (AC9). Trả <c>null</c> nếu
    /// Model chưa từng được cấu hình.
    /// </summary>
    Task<PackingModelConfigDto?> GetByModelAsync(string model, CancellationToken cancellationToken = default);

    /// <summary>Gợi ý autocomplete các Model đã có cấu hình, khớp gần đúng <paramref name="search"/> (AC9) — trả mảng rỗng nếu không khớp Model nào.</summary>
    Task<IReadOnlyList<string>> SuggestModelsAsync(string? search, CancellationToken cancellationToken = default);

    /// <summary>Tạo mới cấu hình cho 1 Model (AC1) — ném <see cref="Domain.Exceptions.BusinessRuleException"/> (409) nếu Model đã có cấu hình (so khớp AC9).</summary>
    Task<PackingModelConfigDto> CreateAsync(CreatePackingModelConfigRequest request, string? updatedByUserName, CancellationToken cancellationToken = default);

    /// <summary>Sửa cấu hình đã có (AC2) — không hồi tố dữ liệu đã có (US-25 xử lý snapshot, US-24 chỉ cần đọc ra giá trị mới).</summary>
    Task<PackingModelConfigDto> UpdateAsync(int id, UpdatePackingModelConfigRequest request, string? updatedByUserName, CancellationToken cancellationToken = default);

    /// <summary>Tải lên (thay thế) file mẫu tem in cho 1 cấu hình (AC4) — chỉ nhận đúng phần mở rộng .xlsx, không rỗng.</summary>
    Task<PackingModelConfigDto> UploadTemplateAsync(int id, Stream content, string fileName, string? updatedByUserName, CancellationToken cancellationToken = default);

    /// <summary>Mở stream tải xuống file mẫu tem đang cấu hình (AC5) — ném <see cref="Domain.Exceptions.EntityNotFoundException"/> nếu chưa từng tải lên.</summary>
    Task<(Stream Content, string FileName)> DownloadTemplateAsync(int id, CancellationToken cancellationToken = default);
}
