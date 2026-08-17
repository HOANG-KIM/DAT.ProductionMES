using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.ProductionPlanStages;

/// <summary>
/// Implementation IProductionPlanStageService (US-05a/FR-05a). Trình tự công đoạn (Stage nào, thứ tự nào) KHÔNG
/// còn thuộc phạm vi service này — đã chuyển hẳn sang <c>LineStageSequenceService</c> (US-03/FR-03, cấu hình
/// của Line, dùng chung mọi kế hoạch). Service này giờ chỉ quản lý vòng đời trạng thái
/// (<see cref="ProductionPlanStage.PlanStatus"/>) và tiến độ "đã chạy/còn lại" (tính động từ lịch sử
/// <see cref="Scan"/> kết quả OK, KHÔNG lưu số liệu tĩnh) của từng cặp (Kế hoạch, Công đoạn).
///
/// Cơ chế "lazy get-or-create": bản ghi <see cref="ProductionPlanStage"/> của 1 cặp (Kế hoạch, Công đoạn) chỉ
/// được tạo (persist, trạng thái <c>Draft</c>) khi lần đầu cần đọc/thao tác tới đúng cặp đó — vì MỌI Stage trong
/// trình tự của Line đã tự động áp dụng cho MỌI kế hoạch trên Line đó (US-03), không cần bước "gắn công đoạn vào
/// kế hoạch" thủ công như thiết kế cũ.
/// </summary>
public class ProductionPlanStageService : IProductionPlanStageService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductionPlanStageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProductionPlanStageDto>> GetByProductionPlanAsync(int productionPlanId, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);
        var lineSequence = await GetLineSequenceAsync(productionPlan.LineId, cancellationToken);

        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var existingItems = (await repository.FindAsync(x => x.ProductionPlanId == productionPlanId, cancellationToken)).ToList();
        var existingStageIds = existingItems.Select(x => x.StageId).ToHashSet();

        // Get-or-create: mọi Stage trong trình tự của Line mà kế hoạch này CHƯA có bản ghi vòng đời -> tạo mới Draft.
        var missing = lineSequence.Where(x => !existingStageIds.Contains(x.StageId)).ToList();
        if (missing.Count > 0)
        {
            foreach (var lineStage in missing)
            {
                var newItem = new ProductionPlanStage
                {
                    ProductionPlanId = productionPlanId,
                    StageId = lineStage.StageId,
                    LineId = productionPlan.LineId,
                    PlanStatus = PlanStatus.Draft,
                };
                await repository.AddAsync(newItem, cancellationToken);
                existingItems.Add(newItem);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await ToDtoListAsync(existingItems, lineSequence, productionPlan, cancellationToken);
    }

    public async Task<ProductionPlanStageDto> ApplyAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);
        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var (item, lineSequence) = await GetOrCreatePlanStageAsync(productionPlan, stageId, cancellationToken);

        // AC7: Completed/Cancelled không tự "Áp dụng" lại được như Paused.
        if (item.PlanStatus is PlanStatus.Completed or PlanStatus.Cancelled)
        {
            throw new BusinessRuleException(
                $"Kế hoạch tại công đoạn này đã kết thúc vòng đời ({item.PlanStatus}) — không thể Áp dụng lại.");
        }

        if (item.PlanStatus == PlanStatus.Running)
        {
            // Đã Running sẵn -> coi như idempotent, không cần kiểm tra thêm.
            return (await ToDtoListAsync(new[] { item }, lineSequence, productionPlan, cancellationToken)).Single();
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

        return (await ToDtoListAsync(new[] { item }, lineSequence, productionPlan, cancellationToken)).Single();
    }

    public async Task<ProductionPlanStageDto> PauseAsync(int productionPlanId, int stageId, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);
        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var (item, lineSequence) = await GetOrCreatePlanStageAsync(productionPlan, stageId, cancellationToken);

        if (item.PlanStatus != PlanStatus.Running)
        {
            throw new BusinessRuleException(
                $"Chỉ có thể Tạm dừng khi đang Running (trạng thái hiện tại: {item.PlanStatus}).");
        }

        // AC3: chuyển Paused, tiến độ không mất vì tính động từ lịch sử scan OK (không sửa gì khác).
        item.PlanStatus = PlanStatus.Paused;
        repository.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await ToDtoListAsync(new[] { item }, lineSequence, productionPlan, cancellationToken)).Single();
    }

    public async Task<ProductionPlanStageDto> CloseAsync(int productionPlanId, int stageId, CloseProductionPlanStageRequest request, CancellationToken cancellationToken = default)
    {
        var productionPlan = await GetProductionPlanOrThrowAsync(productionPlanId, cancellationToken);
        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var (item, lineSequence) = await GetOrCreatePlanStageAsync(productionPlan, stageId, cancellationToken);

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

        return (await ToDtoListAsync(new[] { item }, lineSequence, productionPlan, cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<ProductionPlanStageSelectionDto>> GetByLineAndStageAsync(
        int lineId, int stageId, bool includeClosed = false, CancellationToken cancellationToken = default)
    {
        // US-03 (17/08/2026): mọi kế hoạch tạo trên 1 Line đều tự động áp dụng ĐÚNG VÀ ĐỦ toàn bộ trình tự đã
        // cấu hình cho Line đó — nên liệt kê MỌI ProductionPlan của Line, không còn phụ thuộc việc đã tồn tại
        // bản ghi ProductionPlanStage hay chưa (left-join "ảo": mặc định Draft nếu chưa có bản ghi thật).
        var plans = await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => p.LineId == lineId, cancellationToken);
        if (plans.Count == 0)
        {
            return Array.Empty<ProductionPlanStageSelectionDto>();
        }

        var planIds = plans.Select(p => p.Id).ToList();

        var planStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
            x => x.LineId == lineId && x.StageId == stageId, cancellationToken);
        var planStatusByPlanId = planStages.ToDictionary(x => x.ProductionPlanId, x => x.PlanStatus);

        // US-05a AC4: "đã chạy" tính động từ lịch sử scan OK, đúng công đoạn này, gộp 1 truy vấn cho toàn bộ danh sách.
        var okScans = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.StageId == stageId && s.Result == ScanResult.Ok && planIds.Contains(s.ProductionPlanId), cancellationToken);
        var runCountByPlanId = okScans.GroupBy(s => s.ProductionPlanId).ToDictionary(g => g.Key, g => g.Count());

        return plans
            .Select(plan =>
            {
                var planStatus = planStatusByPlanId.TryGetValue(plan.Id, out var status) ? status : PlanStatus.Draft;
                return (plan, planStatus);
            })
            // US-05a AC7: mặc định ẩn các cặp đã kết thúc vòng đời tại công đoạn này.
            .Where(x => includeClosed || x.planStatus is not (PlanStatus.Completed or PlanStatus.Cancelled))
            .OrderBy(x => x.plan.StartTime)
            .Select(x =>
            {
                var runCount = runCountByPlanId.TryGetValue(x.plan.Id, out var count) ? count : 0;
                return new ProductionPlanStageSelectionDto
                {
                    ProductionPlanId = x.plan.Id,
                    StageId = stageId,
                    LineId = lineId,
                    Customer = x.plan.Customer,
                    Model = x.plan.Model,
                    Lot = x.plan.Lot,
                    Revision = x.plan.Revision,
                    PlannedQuantity = x.plan.PlannedQuantity,
                    TaktTimeSeconds = x.plan.TaktTimeSeconds,
                    StandardQuantityPerHour = x.plan.TaktTimeSeconds > 0 ? Math.Round(3600m / x.plan.TaktTimeSeconds, 2) : 0,
                    StartTime = x.plan.StartTime,
                    OperatorNames = x.plan.OperatorNames,
                    PlanStatus = x.planStatus,
                    RunCount = runCount,
                    RemainingCount = Math.Max(0, x.plan.PlannedQuantity - runCount),
                };
            })
            .ToList();
    }

    private async Task<ProductionPlan> GetProductionPlanOrThrowAsync(int productionPlanId, CancellationToken cancellationToken)
    {
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(productionPlanId, cancellationToken);
        return productionPlan ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {productionPlanId}.");
    }

    /// <summary>Trình tự công đoạn của Line (US-03), sắp theo SequenceNumber — nguồn sự thật cho "Stage nào thuộc Line này".</summary>
    private async Task<List<LineStageSequence>> GetLineSequenceAsync(int lineId, CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<LineStageSequence>().FindAsync(x => x.LineId == lineId, cancellationToken);
        return items.OrderBy(x => x.SequenceNumber).ToList();
    }

    /// <summary>
    /// Lazy get-or-create: trả về bản ghi vòng đời của cặp (Kế hoạch, Công đoạn), tạo mới Draft nếu chưa có, kèm
    /// toàn bộ trình tự công đoạn của Line (dùng suy PreviousStageId). Ném <see cref="EntityNotFoundException"/>
    /// nếu <paramref name="stageId"/> không thuộc trình tự đã cấu hình cho Line của kế hoạch này.
    /// </summary>
    private async Task<(ProductionPlanStage Item, List<LineStageSequence> LineSequence)> GetOrCreatePlanStageAsync(
        ProductionPlan productionPlan, int stageId, CancellationToken cancellationToken)
    {
        var lineSequence = await GetLineSequenceAsync(productionPlan.LineId, cancellationToken);
        if (lineSequence.All(x => x.StageId != stageId))
        {
            throw new EntityNotFoundException("Công đoạn này không thuộc trình tự cấu hình của Line.");
        }

        var repository = _unitOfWork.Repository<ProductionPlanStage>();
        var existingItems = await repository.FindAsync(x => x.ProductionPlanId == productionPlan.Id, cancellationToken);
        var item = existingItems.FirstOrDefault(x => x.StageId == stageId);

        if (item is null)
        {
            item = new ProductionPlanStage
            {
                ProductionPlanId = productionPlan.Id,
                StageId = stageId,
                LineId = productionPlan.LineId,
                PlanStatus = PlanStatus.Draft,
            };
            await repository.AddAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return (item, lineSequence);
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
    /// Suy ra PreviousStageId từ trình tự công đoạn CỦA LINE (<paramref name="lineSequence"/>, FR-03/FR-08) và
    /// tính tiến độ "đã chạy/còn lại" động cho từng công đoạn (US-05a AC4) — 1 truy vấn Scan duy nhất cho toàn bộ
    /// danh sách công đoạn của kế hoạch.
    /// </summary>
    private async Task<List<ProductionPlanStageDto>> ToDtoListAsync(
        IEnumerable<ProductionPlanStage> items, List<LineStageSequence> lineSequence, ProductionPlan productionPlan, CancellationToken cancellationToken)
    {
        var sequenceByStageId = lineSequence.ToDictionary(x => x.StageId, x => x.SequenceNumber);
        var stageIdBySequenceNumber = lineSequence.ToDictionary(x => x.SequenceNumber, x => x.StageId);

        var okScans = await _unitOfWork.Repository<Scan>().FindAsync(
            s => s.ProductionPlanId == productionPlan.Id && s.Result == ScanResult.Ok, cancellationToken);
        var runCountByStageId = okScans.GroupBy(s => s.StageId).ToDictionary(g => g.Key, g => g.Count());

        return items
            .OrderBy(x => sequenceByStageId.TryGetValue(x.StageId, out var seq) ? seq : int.MaxValue)
            .Select(x =>
            {
                var runCount = runCountByStageId.TryGetValue(x.StageId, out var count) ? count : 0;
                var sequenceNumber = sequenceByStageId.TryGetValue(x.StageId, out var seq) ? seq : 0;
                var previousStageId = stageIdBySequenceNumber.TryGetValue(sequenceNumber - 1, out var previous) ? previous : (int?)null;
                return new ProductionPlanStageDto
                {
                    Id = x.Id,
                    ProductionPlanId = x.ProductionPlanId,
                    StageId = x.StageId,
                    SequenceNumber = sequenceNumber,
                    PreviousStageId = previousStageId,
                    PlanStatus = x.PlanStatus,
                    RunCount = runCount,
                    RemainingCount = Math.Max(0, productionPlan.PlannedQuantity - runCount),
                };
            }).ToList();
    }
}
