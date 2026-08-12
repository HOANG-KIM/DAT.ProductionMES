using ProductionMES.Application.DTOs.WorkStations;

namespace ProductionMES.Application.Services.WorkStations;

/// <summary>Service quản lý trạm làm việc (US-04/FR-04).</summary>
public interface IWorkStationService
{
    /// <summary>Tạo mới 1 trạm, gắn đúng 1 Line và 1 công đoạn đã tồn tại (AC1).</summary>
    Task<WorkStationDto> CreateAsync(CreateWorkStationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật thông tin 1 trạm đã tồn tại.</summary>
    Task<WorkStationDto> UpdateAsync(int id, UpdateWorkStationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Vô hiệu hóa trạm (soft-delete qua cờ hoạt động, không xóa cứng bản ghi).</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);

    Task<WorkStationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkStationDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
