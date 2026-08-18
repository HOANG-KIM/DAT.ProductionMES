using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ReworkUnlocks;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.ReworkUnlocks;

/// <summary>Implementation IReworkUnlockService (US-19 AC2/AC6, FR-19).</summary>
public class ReworkUnlockService : IReworkUnlockService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReworkUnlockService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReworkUnlockDto> UnlockAsync(
        int workStationId, string tagCode, string? note, int unlockedByUserId, string unlockedByUserName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            throw new BusinessRuleException("Mã tem không được để trống.");
        }

        // Phòng vệ Service bị gọi trực tiếp không qua Controller (cùng idiom ScanService.CreateNgAsync, US-18).
        if (unlockedByUserId <= 0 || string.IsNullOrWhiteSpace(unlockedByUserName))
        {
            throw new BusinessRuleException("Thiếu thông tin người thực hiện Mở khóa rework (yêu cầu đăng nhập Tổ trưởng hợp lệ).");
        }

        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");

        tagCode = tagCode.Trim();
        var stageId = workStation.StageId;

        var scansAtStage = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.TagCode == tagCode && s.StageId == stageId, cancellationToken);
        var unlocksAtStage = await _unitOfWork.Repository<ReworkUnlock>().FindAsync(
            u => u.TagCode == tagCode && u.StageId == stageId, cancellationToken);

        if (!ReworkLockCalculator.IsLocked(scansAtStage, unlocksAtStage))
        {
            throw new BusinessRuleException(
                $"Tem \"{tagCode}\" hiện không bị khóa rework tại công đoạn của trạm Id = {workStationId} — không thể thực hiện Mở khóa rework.");
        }

        var unlock = new ReworkUnlock
        {
            TagCode = tagCode,
            StageId = stageId,
            UnlockedByUserId = unlockedByUserId,
            UnlockedByUserName = unlockedByUserName,
            UnlockedAtUtc = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };

        await _unitOfWork.Repository<ReworkUnlock>().AddAsync(unlock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReworkUnlockDto
        {
            Id = unlock.Id,
            TagCode = unlock.TagCode,
            StageId = unlock.StageId,
            UnlockedByUserId = unlock.UnlockedByUserId,
            UnlockedByUserName = unlock.UnlockedByUserName,
            UnlockedAtUtc = unlock.UnlockedAtUtc,
            Note = unlock.Note,
        };
    }

    public async Task<ReworkLockStatusDto> GetLockStatusAsync(int workStationId, string tagCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            throw new BusinessRuleException("Mã tem không được để trống.");
        }

        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");

        tagCode = tagCode.Trim();
        var stageId = workStation.StageId;

        var scansAtStage = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.TagCode == tagCode && s.StageId == stageId, cancellationToken);
        var unlocksAtStage = await _unitOfWork.Repository<ReworkUnlock>().FindAsync(
            u => u.TagCode == tagCode && u.StageId == stageId, cancellationToken);

        var ngScans = scansAtStage.Where(s => s.Result == ScanResult.Ng).ToList();
        var latestNg = ngScans
            .OrderByDescending(s => s.ScannedAtUtc)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();

        return new ReworkLockStatusDto
        {
            TagCode = tagCode,
            StageId = stageId,
            IsLocked = ReworkLockCalculator.IsLocked(scansAtStage, unlocksAtStage),
            HasNgHistory = latestNg is not null,
            RejectionReason = latestNg?.RejectionReason,
            NgScannedAtUtc = latestNg?.ScannedAtUtc,
            NgConfirmedByUserName = latestNg?.ConfirmedByUserName,
            NgCount = ngScans.Count,
        };
    }
}
