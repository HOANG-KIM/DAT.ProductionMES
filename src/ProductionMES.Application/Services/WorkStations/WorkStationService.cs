using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.WorkStations;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.WorkStations;

/// <summary>
/// Implementation IWorkStationService (US-04/FR-04). Validate FK (Line, Stage phải tồn tại — AC1) và
/// rule cổng COM (AC2/AC3, xử lý ở FluentValidation validator, không lặp lại ở đây).
/// Vô hiệu hóa là soft-delete, cùng pattern với Line/Stage.
/// </summary>
public class WorkStationService : IWorkStationService
{
    private readonly IUnitOfWork _unitOfWork;

    public WorkStationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkStationDto> CreateAsync(CreateWorkStationRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureLineAndStageExistAsync(request.LineId, request.StageId, cancellationToken);

        var workStation = new WorkStation
        {
            Name = request.Name,
            LineId = request.LineId,
            StageId = request.StageId,
            UseArduino = request.UseArduino,
            ComPort = request.UseArduino ? request.ComPort : null,
            BaudRate = request.UseArduino ? request.BaudRate : null,
            CommandProtocol = request.UseArduino ? request.CommandProtocol : null,
            IsActive = true,
        };

        var repository = _unitOfWork.Repository<WorkStation>();
        await repository.AddAsync(workStation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(workStation);
    }

    public async Task<WorkStationDto> UpdateAsync(int id, UpdateWorkStationRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureLineAndStageExistAsync(request.LineId, request.StageId, cancellationToken);

        var repository = _unitOfWork.Repository<WorkStation>();
        var workStation = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {id}.");

        workStation.Name = request.Name;
        workStation.LineId = request.LineId;
        workStation.StageId = request.StageId;
        workStation.UseArduino = request.UseArduino;
        // AC3: trạm không dùng Arduino không lưu thông tin cổng COM
        workStation.ComPort = request.UseArduino ? request.ComPort : null;
        workStation.BaudRate = request.UseArduino ? request.BaudRate : null;
        workStation.CommandProtocol = request.UseArduino ? request.CommandProtocol : null;

        repository.Update(workStation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(workStation);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<WorkStation>();
        var workStation = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {id}.");

        workStation.IsActive = false;

        repository.Update(workStation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkStationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(id, cancellationToken);
        return workStation is null ? null : ToDto(workStation);
    }

    public async Task<IReadOnlyList<WorkStationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var workStations = await _unitOfWork.Repository<WorkStation>().GetAllAsync(cancellationToken);
        return workStations.Select(ToDto).ToList();
    }

    private async Task EnsureLineAndStageExistAsync(int lineId, int stageId, CancellationToken cancellationToken)
    {
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(lineId, cancellationToken);
        if (line is null)
        {
            throw new EntityNotFoundException($"Không tìm thấy Line với Id = {lineId}.");
        }

        var stage = await _unitOfWork.Repository<Stage>().GetByIdAsync(stageId, cancellationToken);
        if (stage is null)
        {
            throw new EntityNotFoundException($"Không tìm thấy công đoạn với Id = {stageId}.");
        }
    }

    private static WorkStationDto ToDto(WorkStation workStation) => new()
    {
        Id = workStation.Id,
        Name = workStation.Name,
        LineId = workStation.LineId,
        StageId = workStation.StageId,
        UseArduino = workStation.UseArduino,
        ComPort = workStation.ComPort,
        BaudRate = workStation.BaudRate,
        CommandProtocol = workStation.CommandProtocol,
        IsActive = workStation.IsActive,
    };
}
