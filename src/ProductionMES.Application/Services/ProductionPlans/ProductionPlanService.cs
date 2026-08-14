using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.ProductionPlans;

/// <summary>
/// Implementation IProductionPlanService (US-05/FR-05, cập nhật 13/08/2026).
/// US-06/FR-06: StandardQuantityPerHour tính lúc map Entity → DTO (ToDto), không lưu cột riêng trong DB.
/// Vòng đời trạng thái (Draft/Running/Paused/Completed/Cancelled) KHÔNG xử lý ở service này — xem
/// <see cref="ProductionPlanStages.ProductionPlanStageService"/> (US-05a, đúng entity đang giữ trạng thái đó).
/// </summary>
public class ProductionPlanService : IProductionPlanService
{
    private const decimal SecondsPerHour = 3600m;

    private readonly IUnitOfWork _unitOfWork;

    public ProductionPlanService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductionPlanDto> CreateAsync(CreateProductionPlanRequest request, CancellationToken cancellationToken = default)
    {
        // AC1: Line phải tồn tại và đang hoạt động mới được tạo kế hoạch
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(request.LineId, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy Line với Id = {request.LineId}.");

        if (!line.IsActive)
        {
            throw new BusinessRuleException($"Line \"{line.Name}\" đang ngưng hoạt động, không thể tạo kế hoạch sản xuất mới.");
        }

        var productionPlan = new ProductionPlan
        {
            LineId = request.LineId,
            Customer = request.Customer,
            Model = request.Model,
            Lot = request.Lot,
            Revision = request.Revision,
            PlannedQuantity = request.PlannedQuantity,
            TaktTimeSeconds = request.TaktTimeSeconds,
            StartTime = request.StartTime,
            OperatorNames = request.OperatorNames,
        };

        var repository = _unitOfWork.Repository<ProductionPlan>();
        await repository.AddAsync(productionPlan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(productionPlan);
    }

    public async Task<ProductionPlanDto> UpdateAsync(int id, UpdateProductionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<ProductionPlan>();
        var productionPlan = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy kế hoạch sản xuất với Id = {id}.");

        // AC5: sửa Số lượng kế hoạch/Takt time trong khi kế hoạch đã có công đoạn Running/Paused là tình huống
        // "chạy dở" — cần Tổ trưởng xác nhận rõ ràng trước khi lưu, tránh sửa nhầm làm sai lệch cách tính "còn
        // lại" (US-05a AC4) hoặc chỉ số +/- đang hiển thị tại trạm.
        var quantityOrTaktTimeChanged =
            request.PlannedQuantity != productionPlan.PlannedQuantity ||
            request.TaktTimeSeconds != productionPlan.TaktTimeSeconds;

        if (quantityOrTaktTimeChanged && !request.Confirm)
        {
            var runningOrPausedStages = await _unitOfWork.Repository<ProductionPlanStage>().FindAsync(
                x => x.ProductionPlanId == id && (x.PlanStatus == PlanStatus.Running || x.PlanStatus == PlanStatus.Paused),
                cancellationToken);

            if (runningOrPausedStages.Count > 0)
            {
                throw new BusinessRuleException(
                    "Kế hoạch này đang chạy dở (có công đoạn Running/Paused) — sửa Số lượng kế hoạch/Takt time có " +
                    "thể làm sai lệch cách tính 'còn lại' hoặc chỉ số +/- đang hiển thị tại trạm. Xác nhận thay đổi " +
                    "bằng cách gửi lại yêu cầu với Confirm = true.");
            }
        }

        // AC6 (US-05): khóa TUYỆT ĐỐI sửa Customer/Model/Lot/Revision nếu kế hoạch đã có ít nhất 1 bản ghi Scan
        // — khác hẳn cơ chế Confirm=true ở trên (Quantity/TaktTime), rule này KHÔNG có đường Confirm để bỏ qua,
        // không lọc theo PlanStatus/Result, vì lịch sử scan cũ đã gắn snapshot (US-10) với đúng giá trị hiện tại,
        // sửa sẽ làm traceability sai lệch giữa "kế hoạch hiện tại" và "kế hoạch tại thời điểm đã scan".
        var identityFieldsChanged =
            request.Customer != productionPlan.Customer ||
            request.Model != productionPlan.Model ||
            request.Lot != productionPlan.Lot ||
            request.Revision != productionPlan.Revision;

        if (identityFieldsChanged)
        {
            var existingScans = await _unitOfWork.Repository<Scan>().FindAsync(
                s => s.ProductionPlanId == id, cancellationToken);

            if (existingScans.Count > 0)
            {
                throw new BusinessRuleException(
                    "Kế hoạch này đã có lịch sử scan gắn với Khách hàng/Model/Lot/Revision hiện tại — không thể " +
                    "sửa các thông tin này để tránh sai lệch dữ liệu lịch sử. Nếu nhập sai, hãy tạo kế hoạch mới.");
            }
        }

        // AC4: kế hoạch chưa từng Running ở công đoạn nào (hoặc không đổi Số lượng/Takt time) -> cập nhật tự do.
        productionPlan.Customer = request.Customer;
        productionPlan.Model = request.Model;
        productionPlan.Lot = request.Lot;
        productionPlan.Revision = request.Revision;
        productionPlan.PlannedQuantity = request.PlannedQuantity;
        productionPlan.TaktTimeSeconds = request.TaktTimeSeconds;
        productionPlan.StartTime = request.StartTime;
        productionPlan.OperatorNames = request.OperatorNames;

        repository.Update(productionPlan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(productionPlan);
    }

    public async Task<ProductionPlanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var productionPlan = await _unitOfWork.Repository<ProductionPlan>().GetByIdAsync(id, cancellationToken);
        return productionPlan is null ? null : ToDto(productionPlan);
    }

    public async Task<IReadOnlyList<ProductionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var productionPlans = await _unitOfWork.Repository<ProductionPlan>().GetAllAsync(cancellationToken);
        return productionPlans.Select(ToDto).ToList();
    }

    /// <summary>US-06/FR-06/AC-04: Sản lượng chuẩn/giờ = 3600 / Takt time (vd takt = 30s -> 120 sản phẩm/giờ).</summary>
    private static ProductionPlanDto ToDto(ProductionPlan productionPlan) => new()
    {
        Id = productionPlan.Id,
        LineId = productionPlan.LineId,
        Customer = productionPlan.Customer,
        Model = productionPlan.Model,
        Lot = productionPlan.Lot,
        Revision = productionPlan.Revision,
        PlannedQuantity = productionPlan.PlannedQuantity,
        TaktTimeSeconds = productionPlan.TaktTimeSeconds,
        StartTime = productionPlan.StartTime,
        OperatorNames = productionPlan.OperatorNames,
        StandardQuantityPerHour = productionPlan.TaktTimeSeconds > 0 ? Math.Round(SecondsPerHour / productionPlan.TaktTimeSeconds, 2) : 0,
    };
}
