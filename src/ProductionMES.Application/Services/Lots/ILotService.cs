using ProductionMES.Application.DTOs.Lots;

namespace ProductionMES.Application.Services.Lots;

/// <summary>
/// Quản lý entity <see cref="Domain.Entities.Lot"/> — "Tổng số lượng Lot" NHẬP TAY, dùng chung 1 nguồn duy nhất
/// giữa mọi kế hoạch cùng Lot (US-21a/FR-21a, viết lại hoàn toàn 19/08/2026; US-05 AC7-AC9).
/// </summary>
public interface ILotService
{
    /// <summary>Lấy Lot theo Code — trả <c>null</c> nếu chưa từng có ai nhập "Tổng số lượng Lot" cho Code này (row Lot chưa tồn tại, tạo lazy).</summary>
    Task<LotDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Tra nhiều Lot cùng lúc theo tập hợp Code (tránh N+1) — dùng ở <c>ProductionPlanStageService</c> (US-05b/US-21a AC7) và các nơi cần liệt kê nhiều Lot.</summary>
    Task<IReadOnlyDictionary<string, LotDto>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);

    /// <summary>US-05 AC7 (=US-21a AC1): true nếu Code này đã từng có ít nhất 1 <see cref="Domain.Entities.ProductionPlan"/> (bất kỳ Line/trạng thái) — dùng để xác định "Lot hoàn toàn mới".</summary>
    Task<bool> HasAnyProductionPlanAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert "Tổng số lượng Lot" theo Code (tạo mới nếu chưa có row Lot, cập nhật nếu đã có — US-21a AC1/AC2).
    /// Soft-confirm (US-21a AC3 / US-05 AC8): nếu Lot đã có ≥1 dòng (Line, Công đoạn) với OkCount lớn hơn
    /// <paramref name="totalQuantity"/> và <paramref name="confirm"/> = false, ném <see cref="Domain.Exceptions.BusinessRuleException"/>
    /// nêu rõ Line/Công đoạn vi phạm — KHÔNG chặn cứng tuyệt đối, gọi lại với <paramref name="confirm"/> = true để ghi đè.
    /// </summary>
    Task<LotDto> UpsertTotalQuantityAsync(string code, int totalQuantity, bool confirm, string? updatedByUserName, CancellationToken cancellationToken = default);
}
