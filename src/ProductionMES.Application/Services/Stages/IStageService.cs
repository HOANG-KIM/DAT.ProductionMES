using ProductionMES.Application.DTOs.Stages;

namespace ProductionMES.Application.Services.Stages;

/// <summary>Service quản lý danh mục Công đoạn master (US-02/FR-02).</summary>
public interface IStageService
{
    /// <summary>Tạo mới 1 công đoạn, trạng thái hoạt động mặc định (AC1).</summary>
    Task<StageDto> CreateAsync(CreateStageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật tên/mô tả 1 công đoạn đã tồn tại.</summary>
    Task<StageDto> UpdateAsync(int id, UpdateStageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Vô hiệu hóa công đoạn (soft-delete qua cờ hoạt động, không xóa cứng bản ghi) (AC3).</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);

    Task<StageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StageDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
