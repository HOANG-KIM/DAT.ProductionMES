using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Lines;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Lines;

/// <summary>
/// Implementation ILineService (US-01/FR-01). Vô hiệu hóa Line là soft-delete: chỉ đổi cờ
/// <see cref="Line.IsActive"/>, không xóa cứng bản ghi, để giữ nguyên dữ liệu lịch sử scan/kế hoạch
/// đã gắn với Line (AC3). Thêm/sửa Line có hiệu lực ngay vì là CRUD thuần cấu hình lưu trực tiếp vào DB,
/// không cần deploy lại code (AC4).
/// </summary>
public class LineService : ILineService
{
    private readonly IUnitOfWork _unitOfWork;

    public LineService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LineDto> CreateAsync(CreateLineRequest request, CancellationToken cancellationToken = default)
    {
        var line = new Line
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true, // AC1: trạng thái hoạt động mặc định khi tạo mới
        };

        var repository = _unitOfWork.Repository<Line>();
        await repository.AddAsync(line, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(line);
    }

    public async Task<LineDto> UpdateAsync(int id, UpdateLineRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Line>();
        var line = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy Line với Id = {id}.");

        // AC2: chỉ cập nhật tên/mô tả; không đụng tới trạng thái hoạt động hay dữ liệu lịch sử liên quan
        line.Name = request.Name;
        line.Description = request.Description;

        repository.Update(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(line);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Line>();
        var line = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy Line với Id = {id}.");

        // AC3: vô hiệu hóa = soft-delete, chỉ đổi cờ IsActive, không gọi Remove -> giữ nguyên dữ liệu lịch sử
        line.IsActive = false;

        repository.Update(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<LineDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(id, cancellationToken);
        return line is null ? null : ToDto(line);
    }

    public async Task<IReadOnlyList<LineDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var lines = await _unitOfWork.Repository<Line>().GetAllAsync(cancellationToken);
        return lines.Select(ToDto).ToList();
    }

    private static LineDto ToDto(Line line) => new()
    {
        Id = line.Id,
        Name = line.Name,
        Description = line.Description,
        IsActive = line.IsActive,
    };
}
