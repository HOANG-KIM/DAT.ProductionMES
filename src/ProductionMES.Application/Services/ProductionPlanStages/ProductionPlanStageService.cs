using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.ProductionPlanStages;

/// <summary>
/// Implementation IProductionPlanStageService (US-03/FR-03, US-05a/FR-05a).
/// Mô hình dữ liệu: danh sách tuyến tính (ProductionPlanStage.SequenceNumber duy nhất trong phạm vi 1 kế hoạch), "công đoạn
/// liền trước" suy ra từ SequenceNumber - 1 (xem lý do thiết kế đầy đủ tại <see cref="ProductionPlanStage"/>). Ràng buộc
/// "1 công đoạn không xuất hiện quá 1 lần trong cùng kế hoạch" (kiểm tra ở AddAsync) là điều kiện đảm bảo AC5
/// (không thể tạo vòng lặp) đúng cấu trúc, không cần thuật toán phát hiện chu trình riêng.
///
/// US-05a: vòng đời trạng thái (<see cref="ProductionPlanStage.PlanStatus"/>) và tiến độ "đã chạy/còn lại" (tính động từ
/// lịch sử <see cref="Scan"/> kết quả OK, KHÔNG lưu số liệu tĩnh) đều được quản lý ở đây, vì ProductionPlanStage là entity
/// đại diện đúng cặp (Kế hoạch, Công đoạn) mô tả trong FR-05a.
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
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);

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
            LineId = productionPlan.LineId, // denormalize từ kế hoạch cha (xem remarks tại entity)
            SequenceNumber = sequenceNumber,
            PlanStatus = PlanStatus.Draft,
        };

        await repository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var allItems = existingItems.Append(item).ToList();
        var dtoList = await ToDtoListAsync(allItems, productionPlan, cancellationToken);
        return dtoList.Single(x => x.Id == item.Id);
    }

    public async Task RemoveAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);

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
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);

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

        return await ToDtoListAsync(existingItems, productionPlan, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionPlanStageDto>> GetByProductionPlanAsync(int productionPlanId, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);

        var items = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(x => x.ProductionPlanId == productionPlanId, cancellationToken);
        return await ToDtoListAsync(items, productionPlan, cancellationToken);
    }

    public async Task<ProductionPlanStageDto> ApplyAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);
        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var item = await GetPlanStageOrThrowAsync(repository, productionPlanId, stageId, cancellationToken);

        // AC7: Completed/Cancelled không tự "Áp dụng" lại được như Paused.
        if (item.PlanStatus is PlanStatus.Completed or PlanStatus.Cancelled)
        {
            throw new BusinessRuleException(
                $"Kế hoạch tại công đoạn này đã kết thúc vòng đời ({item.PlanStatus}) — không thể Áp dụng lại.");
        }

        if (item.PlanStatus == PlanStatus.Running)
        {
            // Đã Running sẵn -> coi như idempotent, không cần kiểm tra thêm.
            return (await ToDtoListAsync(new[] { item }, productionPlan, cancellationToken)).Single();
        }

        // AC1/AC2: chỉ chặn nếu ĐÚNG cặp (Line, Công đoạn) này đang có 1 kế hoạch KHÁC ở Running — không ràng
        // buộc theo cả Line (công đoạn khác của cùng Line được chạy kế hoạch khác song song).
        var runningAtSameLineStage = await repository.FindAsync(
            x => x.LineId == productionPlan.LineId && x.StageId == stageId && x.PlanStatus == PlanStatus.Running && x.Id != item.Id,
            cancellationToken);

        if (runningAtSameLineStage.Count > 0)
        {
            throw new BusinessRuleException(
                $"(Line Id = {productionPlan.LineId}, Công đoạn Id = {stageId}) đang có 1 kế hoạch khác " +
                $"(Kế hoạch Id = {runningAtSameLineStage[0].ProductionPlanId}) ở trạng thái Running. Cần Tạm dừng/Đóng " +
                "kế hoạch đó trước khi Áp dụng kế hoạch này.");
        }

        item.PlanStatus = PlanStatus.Running;
        repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await ToDtoListAsync(new[] { item }, productionPlan, cancellationToken)).Single();
    }

    public async Task<ProductionPlanStageDto> PauseAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);
        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var item = await GetPlanStageOrThrowAsync(repository, productionPlanId, stageId, cancellationToken);

        if (item.PlanStatus != PlanStatus.Running)
        {
            throw new BusinessRuleException(
                $"Chỉ có thể Tạm dừng khi đang Running (trạng thái hiện tại: {item.PlanStatus}).");
        }

        // AC3: chuyển Paused, tiến độ không mất vì tính động từ lịch sử scan OK (không sửa gì khác).
        item.PlanStatus = PlanStatus.Paused;
        repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await ToDtoListAsync(new[] { item }, productionPlan, cancellationToken)).Single();
    }

    public async Task<ProductionPlanStageDto> CloseAsync(int productionPlanId, int stageId, CloseProductionPlanStageRequest request, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);
        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var item = await GetPlanStageOrThrowAsync(repository, productionPlanId, stageId, cancellationToken);

        if (item.PlanStatus is PlanStatus.Completed or PlanStatus.Cancelled)
        {
            throw new BusinessRuleException($"Kế hoạch tại công đoạn này đã kết thúc vòng đời ({item.PlanStatus}).");
        }

        if (item.PlanStatus is not (PlanStatus.Running or PlanStatus.Paused))
        {
            throw new BusinessRuleException(
                $"Chỉ có thể Đóng kế hoạch khi đang Running hoặc Paused (trạng thái hiện tại: {item.PlanStatus}).");
        }

        var runCount = await GetRunCountAsync(productionPlanId, stageId, cancellationToken);

        // AC6: yêu cầu xác nhận nếu số lượng thực tế còn thấp hơn kế hoạch.
        if (runCount < productionPlan.PlannedQuantity && !request.Confirm)
        {
            throw new BusinessRuleException(
                $"Số lượng thực tế mới đạt {runCount}/{productionPlan.PlannedQuantity} (còn thiếu " +
                $"{productionPlan.PlannedQuantity - runCount}). Xác nhận đóng sớm bằng cách gửi lại yêu cầu với Confirm = true.");
        }

        item.PlanStatus = PlanStatus.Cancelled;
        repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await ToDtoListAsync(new[] { item }, productionPlan, cancellationToken)).Single();
    }

    private async Task<ProductionPlan> GetProductionPlanOrThrowAsync(int productionPlanId, CancellationToken cancellationToken)
    {
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(productionPlanId, cancellationToken);
        return productionPlan ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {productionPlanId}.");
    }

    private static async Task<ProductionPlanStage> GetPlanStageOrThrowAsync(
        IRepository<ProductionPlanStage> repository, int productionPlanId, int stageId, CancellationToken cancellationToken)
    {
        var items = await repository.FindAsync(x => x.ProductionPlanId == productionPlanId, cancellationToken);
        return items.FirstOrDefault(x => x.StageId == stageId)
            ?? throw new EntityNotFoundException($"Công đoạn Id = {stageId} chưa được cấu hình trong kế hoạch Id = {productionPlanId}.");
    }

    /// <summary>US-05a AC4: "đã chạy" = tổng số lượt scan kết quả OK theo đúng cặp (Kế hoạch, Công đoạn), tính động.</summary>
    private async Task<int> GetRunCountAsync(int productionPlanId, int stageId, CancellationToken cancellationToken)
    {
        var okScans = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.ProductionPlanId == productionPlanId && s.StageId == stageId && s.Result == ScanResult.Ok,
            cancellationToken);
        return okScans.Count;
    }

    /// <summary>
    /// Suy ra PreviousStageId từ SequenceNumber - 1 trong cùng kế hoạch (FR-03/FR-08) và tính tiến độ "đã
    /// chạy/còn lại" động cho từng công đoạn (US-05a AC4) — 1 truy vấn Scan duy nhất cho toàn bộ danh sách.
    /// </summary>
    private async Task<List<ProductionPlanStageDto>> ToDtoListAsync(
        IEnumerable<ProductionPlanStage> items, ProductionPlan productionPlan, CancellationToken cancellationToken)
    {
        var ordered = items.OrderBy(x => x.SequenceNumber).ToList();
        var bySequenceNumber = ordered.ToDictionary(x => x.SequenceNumber);

        var okScans = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.ProductionPlanId == productionPlan.Id && s.Result == ScanResult.Ok, cancellationToken);
        var runCountByStageId = okScans.GroupBy(s => s.StageId).ToDictionary(g => g.Key, g => g.Count());

        return ordered.Select(x =>
        {
            var runCount = runCountByStageId.TryGetValue(x.StageId, out var count) ? count : 0;
            return new ProductionPlanStageDto
            {
                Id = x.Id,
                ProductionPlanId = x.ProductionPlanId,
                StageId = x.StageId,
                SequenceNumber = x.SequenceNumber,
                PreviousStageId = bySequenceNumber.TryGetValue(x.SequenceNumber - 1, out var previous) ? previous.StageId : null,
                PlanStatus = x.PlanStatus,
                RunCount = runCount,
                RemainingCount = Math.Max(0, productionPlan.PlannedQuantity - runCount),
            };
        }).ToList();
    }
}
