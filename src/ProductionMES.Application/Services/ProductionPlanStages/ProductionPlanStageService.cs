using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.ProductionPlanStages;

/// <summary>
/// Implementation IProductionPlanStageService (US-03/FR-03).
/// Mô hình dữ liệu: danh sách tuyến tính (ProductionPlanStage.SequenceNumber duy nhất trong phạm vi 1 kế hoạch), "công đoạn
/// liền trước" suy ra từ SequenceNumber - 1 (xem lý do thiết kế đầy đủ tại <see cref="ProductionPlanStage"/>). Ràng buộc
/// "1 công đoạn không xuất hiện quá 1 lần trong cùng kế hoạch" (kiểm tra ở AddAsync) là điều kiện đảm bảo AC5
/// (không thể tạo vòng lặp) đúng cấu trúc, không cần thuật toán phát hiện chu trình riêng.
/// </summary>
public class ProductionPlanStageService : IProductionPlanStageService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductionPlanStageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductionPlanStageDto> AddAsync(int productionPlanId, AddStageToProductionPlanRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureProductionPlanExistsAsync(productionPlanId, cancellationToken);

        var stage = await _unitOfWork.Repository<Stage>().GetByIdAsync(request.StageId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy công đoạn với Id = {request.StageId}.");

        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var existingItems = await repository.FindAsync(x => x.ProductionPlanId == productionPlanId, cancellationToken);

        // AC5: 1 công đoạn không được xuất hiện quá 1 lần trong cùng 1 kế hoạch — điều kiện cấu trúc đảm bảo
        // không thể hình thành vòng lặp khi suy ra "liền trước" từ SequenceNumber - 1 (xem remarks tại entity).
        if (existingItems.Any(x => x.StageId == request.StageId))
        {
            throw new BusinessRuleException(
                $"Công đoạn \"{stage.Name}\" đã có trong kế hoạch này, không thể thêm trùng (sẽ tạo vòng lặp trình tự).");
        }

        int sequenceNumber;
        if (request.SequenceNumber.HasValue)
        {
            // AC4: từ chối khi trùng số thứ tự
            if (existingItems.Any(x => x.SequenceNumber == request.SequenceNumber.Value))
            {
                throw new BusinessRuleException($"Số thứ tự {request.SequenceNumber.Value} đã được dùng trong kế hoạch này.");
            }

            sequenceNumber = request.SequenceNumber.Value;
        }
        else
        {
            // AC1: chưa chỉ định trình tự -> mặc định thêm vào cuối danh sách
            sequenceNumber = existingItems.Count == 0 ? 1 : existingItems.Max(x => x.SequenceNumber) + 1;
        }

        var item = new ProductionPlanStage
        {
            ProductionPlanId = productionPlanId,
            StageId = request.StageId,
            SequenceNumber = sequenceNumber,
        };

        await repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var allItems = existingItems.Append(item).ToList();
        return ToDtoList(allItems).Single(x => x.Id == item.Id);
    }

    public async Task RemoveAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        await EnsureProductionPlanExistsAsync(productionPlanId, cancellationToken);

        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var existingItems = await repository.FindAsync(x => x.ProductionPlanId == productionPlanId, cancellationToken);

        var item = existingItems.FirstOrDefault(x => x.StageId == stageId)
            ?? throw new EntityNotFoundException($"Công đoạn Id = {stageId} chưa được cấu hình trong kế hoạch Id = {productionPlanId}.");

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

    public async Task<IReadOnlyList<ProductionPlanStageDto>> ReorderAsync(int productionPlanId, ReorderProductionPlanStageRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureProductionPlanExistsAsync(productionPlanId, cancellationToken);

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

        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var existingItems = await repository.FindAsync(x => x.ProductionPlanId == productionPlanId, cancellationToken);

        if (request.Items.Count != existingItems.Count)
        {
            throw new BusinessRuleException(
                "Danh sách sắp xếp phải bao gồm đầy đủ và chỉ các công đoạn hiện đang thuộc kế hoạch này.");
        }

        var itemsByStageId = existingItems.ToDictionary(x => x.StageId);
        foreach (var reorderItem in request.Items)
        {
            if (!itemsByStageId.TryGetValue(reorderItem.StageId, out _))
            {
                throw new BusinessRuleException(
                    $"Công đoạn Id = {reorderItem.StageId} chưa thuộc kế hoạch này, không thể sắp xếp trình tự.");
            }
        }

        // Cập nhật 2 bước để tránh vi phạm tạm thời ràng buộc unique(ProductionPlanId, SequenceNumber) khi hoán đổi vị
        // trí (vd đổi chỗ 2 công đoạn cho nhau): bước 1 gán SequenceNumber tạm thời âm và duy nhất (dựa trên Id, vốn đã
        // unique) để giải phóng toàn bộ giá trị SequenceNumber dương hiện tại; bước 2 mới gán đúng SequenceNumber cuối cùng theo
        // yêu cầu. Cả 2 bước dùng chung UnitOfWork/DbContext hiện tại của request.
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

    public async Task<IReadOnlyList<ProductionPlanStageDto>> GetByProductionPlanAsync(int productionPlanId, CancellationToken cancellationToken = default)
    {
        await EnsureProductionPlanExistsAsync(productionPlanId, cancellationToken);

        var items = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(x => x.ProductionPlanId == productionPlanId, cancellationToken);
        return ToDtoList(items);
    }

    private async Task EnsureProductionPlanExistsAsync(int productionPlanId, CancellationToken cancellationToken)
    {
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(productionPlanId, cancellationToken);
        if (productionPlan is null)
        {
            throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {productionPlanId}.");
        }
    }

    /// <summary>Suy ra PreviousStageId từ SequenceNumber - 1 trong cùng kế hoạch (FR-03/FR-08).</summary>
    private static List<ProductionPlanStageDto> ToDtoList(IEnumerable<ProductionPlanStage> items)
    {
        var ordered = items.OrderBy(x => x.SequenceNumber).ToList();
        var bySequenceNumber = ordered.ToDictionary(x => x.SequenceNumber);

        return ordered.Select(x => new ProductionPlanStageDto
        {
            Id = x.Id,
            ProductionPlanId = x.ProductionPlanId,
            StageId = x.StageId,
            SequenceNumber = x.SequenceNumber,
            PreviousStageId = bySequenceNumber.TryGetValue(x.SequenceNumber - 1, out var previous) ? previous.StageId : null,
        }).ToList();
    }
}
