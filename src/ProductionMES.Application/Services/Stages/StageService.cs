using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Stages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Stages;

/// <summary>
/// Implementation IStageService (US-02/FR-02). Cùng pattern với LineService (US-01): vô hiệu hóa là
/// soft-delete qua cờ <see cref="Stage.IsActive"/>, không xóa cứng bản ghi (AC3).
/// </summary>
public class StageService : IStageService
{
    private readonly IUnitOfWork _unitOfWork;

    public StageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StageDto> CreateAsync(CreateStageRequest request, CancellationToken cancellationToken = default)
    {
        var stage = new Stage
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true, // AC1: trạng thái hoạt động mặc định khi tạo mới
            IsPackingStage = request.IsPackingStage, // US-25
        };

        var repository = _unitOfWork.Repository<Stage>();
        await repository.AddAsync(stage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(stage);
    }

    public async Task<StageDto> UpdateAsync(int id, UpdateStageRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Stage>();
        var stage = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy công đoạn với Id = {id}.");

        stage.Name = request.Name;
        stage.Description = request.Description;
        stage.IsPackingStage = request.IsPackingStage; // US-25

        repository.Update(stage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(stage);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<Stage>();
        var stage = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy công đoạn với Id = {id}.");

        // AC3: vô hiệu hóa = soft-delete, không còn xuất hiện khi cấu hình kế hoạch mới (FR-03)
        stage.IsActive = false;

        repository.Update(stage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<StageDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var stage = await _unitOfWork.Repository<Stage>().GetByIdAsync(id, cancellationToken);
        return stage is null ? null : ToDto(stage);
    }

    public async Task<IReadOnlyList<StageDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stages = await _unitOfWork.Repository<Stage>().GetAllAsync(cancellationToken);
        return stages.Select(ToDto).ToList();
    }

    private static StageDto ToDto(Stage stage) => new()
    {
        Id = stage.Id,
        Name = stage.Name,
        Description = stage.Description,
        IsActive = stage.IsActive,
        IsPackingStage = stage.IsPackingStage,
    };
}
