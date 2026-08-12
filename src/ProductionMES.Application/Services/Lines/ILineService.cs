using ProductionMES.Application.DTOs.Lines;

namespace ProductionMES.Application.Services.Lines;

/// <summary>Service quản lý danh mục Line sản xuất (US-01/FR-01).</summary>
public interface ILineService
{
    /// <summary>Tạo mới 1 Line, trạng thái hoạt động mặc định (AC1).</summary>
    Task<LineDto> CreateAsync(CreateLineRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật tên/mô tả 1 Line đã tồn tại, không ảnh hưởng dữ liệu lịch sử liên quan (AC2).</summary>
    Task<LineDto> UpdateAsync(int id, UpdateLineRequest request, CancellationToken cancellationToken = default);

    /// <summary>Vô hiệu hóa Line (soft-delete qua cờ hoạt động, không xóa cứng bản ghi) (AC3).</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);

    Task<LineDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LineDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
