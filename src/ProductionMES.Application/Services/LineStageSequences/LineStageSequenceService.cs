using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.LineStageSequences;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.LineStageSequences;

/// <summary>
/// Implementation ILineStageSequenceService (US-03/FR-03).
/// Mô hình dữ liệu: danh sách tuyến tính (LineStageSequence.SequenceNumber duy nhất trong phạm vi 1 Line), "công
/// đoạn liền trước" suy ra từ SequenceNumber - 1 (xem lý do thiết kế đầy đủ tại <see cref="LineStageSequence"/>).
/// Ràng buộc "1 công đoạn không xuất hiện quá 1 lần trong cùng Line" (kiểm tra ở AddAsync) là điều kiện đảm bảo
/// AC5 (không thể tạo vòng lặp) đúng cấu trúc, không cần thuật toán phát hiện chu trình riêng.
/// </summary>
public class LineStageSequenceService : ILineStageSequenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public LineStageSequenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LineStageSequenceDto> AddAsync(int lineId, AddStageToLineRequest request, CancellationToken cancellationToken = default)
    {
        await GetLineOrThrowAsync(lineId, cancellationToken);

        var stage = await _unitOfWork.Repository<Stage>().GetByIdAsync(request.StageId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy công đoạn với Id = {request.StageId}.");

        var repository = _unitOfWork.Repository<LineStageSequence>();
        var existingItems = await repository.FindAsync(x => x.LineId == lineId, cancellationToken);

        // AC5: 1 công đoạn không được xuất hiện quá 1 lần trong cùng 1 Line — điều kiện cấu trúc đảm bảo không
        // thể hình thành vòng lặp khi suy ra "liền trước" từ SequenceNumber - 1 (xem remarks tại entity).
        if (existingItems.Any(x => x.StageId == request.StageId))
        {
            throw new BusinessRuleException(
                $"Công đoạn \"{stage.Name}\" đã có trong trình tự của Line này, không thể thêm trùng (sẽ tạo vòng lặp trình tự).");
        }

        int sequenceNumber;
        if (request.SequenceNumber.HasValue)
        {
            // AC4: từ chối khi trùng số thứ tự
            if (existingItems.Any(x => x.SequenceNumber == request.SequenceNumber.Value))
            {
                throw new BusinessRuleException($"Số thứ tự {request.SequenceNumber.Value} đã được dùng trong trình tự của Line này.");
            }

            sequenceNumber = request.SequenceNumber.Value;
        }
        else
        {
            // AC1: chưa chỉ định trình tự -> mặc định thêm vào cuối danh sách
            sequenceNumber = existingItems.Count == 0 ? 1 : existingItems.Max(x => x.SequenceNumber) + 1;
        }

        var item = new LineStageSequence
        {
            LineId = lineId,
            StageId = request.StageId,
            SequenceNumber = sequenceNumber,
        };

        await repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var allItems = existingItems.Append(item).ToList();
        return ToDtoList(allItems).Single(x => x.Id == item.Id);
    }

    public async Task RemoveAsync(int lineId, int stageId, CancellationToken cancellationToken = default)
    {
        await GetLineOrThrowAsync(lineId, cancellationToken);

        // AC2: chặn hẳn nếu đang có bất kỳ kế hoạch nào của Line đó ở trạng thái Running/Paused tại đúng công
        // đoạn sắp gỡ — phải Tạm dừng/Đóng kế hoạch tại công đoạn đó trước mới gỡ được.
        var activePlanStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            x => x.LineId == lineId && x.StageId == stageId && (x.PlanStatus == PlanStatus.Running || x.PlanStatus == PlanStatus.Paused),
            cancellationToken);

        if (activePlanStages.Count > 0)
        {
            throw new BusinessRuleException(
                $"Đang có {activePlanStages.Count} kế hoạch của Line này ở trạng thái Running/Paused tại công đoạn Id = {stageId}. " +
                "Cần Tạm dừng/Đóng kế hoạch tại công đoạn đó trước khi gỡ khỏi trình tự.");
        }

        var repository = _unitOfWork.Repository<LineStageSequence>();
        var existingItems = await repository.FindAsync(x => x.LineId == lineId, cancellationToken);

        var item = existingItems.FirstOrDefault(x => x.StageId == stageId)
            ?? throw new EntityNotFoundException($"Công đoạn Id = {stageId} chưa được cấu hình trong trình tự của Line Id = {lineId}.");

        repository.Remove(item);

        // AC2: trình tự công đoạn còn lại được điều chỉnh hợp lý — đánh số lại liên tục 1..n theo SequenceNumber hiện tại.
        var remaining = existingItems.Where(x => x.Id != item.Id).OrderBy(x => x.SequenceNumber).ToList();
        for (var i = 0; i < remaining.Count; i++)
        {
            var newSequenceNumber = i + 1;
            if (remaining[i].SequenceNumber != newSequenceNumber)
            {
                remaining[i].SequenceNumber = newSequenceNumber;
                repository.Update(remaining[i]);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LineStageSequenceDto>> ReorderAsync(int lineId, ReorderLineStageSequenceRequest request, CancellationToken cancellationToken = default)
    {
        await GetLineOrThrowAsync(lineId, cancellationToken);

        // AC4: từ chối khi trùng số thứ tự trong danh sách gửi lên
        if (request.Items.Select(x => x.SequenceNumber).Distinct().Count() != request.Items.Count)
        {
            throw new BusinessRuleException("Danh sách trình tự có số thứ tự bị trùng.");
        }

        // AC5: từ chối nếu 1 công đoạn xuất hiện nhiều lần trong danh sách gửi lên (phòng vệ, tránh phá vỡ
        // điều kiện cấu trúc "1 công đoạn - tối đa 1 vị trí" đảm bảo không có vòng lặp).
        if (request.Items.Select(x => x.StageId).Distinct().Count() != request.Items.Count)
        {
            throw new BusinessRuleException("Danh sách trình tự có công đoạn bị lặp lại (sẽ tạo vòng lặp trình tự).");
        }

        var repository = _unitOfWork.Repository<LineStageSequence>();
        var existingItems = await repository.FindAsync(x => x.LineId == lineId, cancellationToken);

        if (request.Items.Count != existingItems.Count)
        {
            throw new BusinessRuleException(
                "Danh sách sắp xếp phải bao gồm đầy đủ và chỉ các công đoạn hiện đang thuộc trình tự của Line này.");
        }

        var itemsByStageId = existingItems.ToDictionary(x => x.StageId);
        foreach (var reorderItem in request.Items)
        {
            if (!itemsByStageId.TryGetValue(reorderItem.StageId, out _))
            {
                throw new BusinessRuleException(
                    $"Công đoạn Id = {reorderItem.StageId} chưa thuộc trình tự của Line này, không thể sắp xếp.");
            }
        }

        // Cập nhật 2 bước để tránh vi phạm tạm thời ràng buộc unique(LineId, SequenceNumber) khi hoán đổi vị trí
        // (vd đổi chỗ 2 công đoạn cho nhau): bước 1 gán SequenceNumber tạm thời âm và duy nhất (dựa trên Id, vốn đã
        // unique) để giải phóng toàn bộ giá trị SequenceNumber dương hiện tại; bước 2 mới gán đúng SequenceNumber
        // cuối cùng theo yêu cầu. Cả 2 bước dùng chung UnitOfWork/DbContext hiện tại của request.
        foreach (var existing in existingItems)
        {
            existing.SequenceNumber = -existing.Id;
            repository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var reorderItem in request.Items)
        {
            var existing = itemsByStageId[reorderItem.StageId];
            existing.SequenceNumber = reorderItem.SequenceNumber;
            repository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDtoList(existingItems);
    }

    public async Task<IReadOnlyList<LineStageSequenceDto>> GetByLineAsync(int lineId, CancellationToken cancellationToken = default)
    {
        await GetLineOrThrowAsync(lineId, cancellationToken);

        var items = await _unitOfWork.Repository<LineStageSequence>().FindAsync(x => x.LineId == lineId, cancellationToken);
        return ToDtoList(items);
    }

    private async Task<Line> GetLineOrThrowAsync(int lineId, CancellationToken cancellationToken)
    {
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(lineId, cancellationToken);
        return line ?? throw new EntityNotFoundException($"Không tìm thấy Line với Id = {lineId}.");
    }

    /// <summary>Suy ra PreviousStageId từ SequenceNumber - 1 trong cùng Line (FR-03/FR-08).</summary>
    private static List<LineStageSequenceDto> ToDtoList(IEnumerable<LineStageSequence> items)
    {
        var ordered = items.OrderBy(x => x.SequenceNumber).ToList();
        var bySequenceNumber = ordered.ToDictionary(x => x.SequenceNumber);

        return ordered.Select(x => new LineStageSequenceDto
        {
            Id = x.Id,
            LineId = x.LineId,
            StageId = x.StageId,
            SequenceNumber = x.SequenceNumber,
            PreviousStageId = bySequenceNumber.TryGetValue(x.SequenceNumber - 1, out var previous) ? previous.StageId : null,
        }).ToList();
    }
}
