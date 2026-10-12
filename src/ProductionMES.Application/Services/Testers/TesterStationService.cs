using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Testers;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Testers;

/// <summary>Implementation <see cref="ITesterStationService"/> (US-11).</summary>
public class TesterStationService : ITesterStationService
{
    private readonly IUnitOfWork _unitOfWork;

    public TesterStationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TesterStationStateDto> GetStateAsync(int workStationId, CancellationToken cancellationToken = default)
    {
        var workStation = await GetWorkStationAsync(workStationId, cancellationToken);
        return ToStateDto(workStation);
    }

    public async Task<TesterStationStateDto> SetEnabledAsync(
        int workStationId, bool enabled, string reason, int approvedByUserId, string approvedByUserName, CancellationToken cancellationToken = default)
    {
        // FluentValidationActionFilter đã validate ở tầng request — kiểm tra lại phòng Service được gọi trực tiếp.
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleException("Phải nhập lý do đổi chế độ máy thông điện.");
        }

        if (approvedByUserId <= 0 || string.IsNullOrWhiteSpace(approvedByUserName))
        {
            throw new BusinessRuleException("Thiếu thông tin người duyệt (yêu cầu đăng nhập Tổ trưởng hợp lệ).");
        }

        var workStation = await GetWorkStationAsync(workStationId, cancellationToken);

        if (!workStation.UseArduino)
        {
            throw new BusinessRuleException("Trạm này không có máy thông điện (UseArduino = false) — không có công tắc để đổi.");
        }

        if (workStation.TesterEnabled == enabled)
        {
            return ToStateDto(workStation);
        }

        var log = new TesterModeChangeLog
        {
            WorkStationId = workStation.Id,
            // Giờ local hệ thống, KHÔNG quy đổi UTC (API-Conventions.md mục 10).
            ChangedAtUtc = DateTime.Now,
            FromMode = workStation.TesterEnabled ? TesterMode.Tester : TesterMode.Bypass,
            ToMode = enabled ? TesterMode.Tester : TesterMode.Bypass,
            Reason = reason.Trim(),
            ApprovedByUserId = approvedByUserId,
            ApprovedByUserName = approvedByUserName,
        };

        workStation.TesterEnabled = enabled;
        _unitOfWork.Repository<WorkStation>().Update(workStation);
        await _unitOfWork.Repository<TesterModeChangeLog>().AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToStateDto(workStation);
    }

    private async Task<WorkStation> GetWorkStationAsync(int workStationId, CancellationToken cancellationToken)
        => await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");

    private static TesterStationStateDto ToStateDto(WorkStation workStation) => new()
    {
        WorkStationId = workStation.Id,
        UseArduino = workStation.UseArduino,
        TesterEnabled = workStation.TesterEnabled,
    };
}
