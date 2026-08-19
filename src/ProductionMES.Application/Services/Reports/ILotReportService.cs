using ProductionMES.Application.DTOs.Reports;

namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// Báo cáo Lot-centric (US-21/FR-21, vòng 3 — 18/08/2026): tìm/chọn 1 Lot rồi xem toàn bộ vòng đời sản xuất của
/// Lot đó, KHÔNG cần biết trước Lot chạy ở Line nào. Khác <see cref="IProductionReportService"/> (bản vòng 1/2,
/// entrypoint là (Line, Công đoạn) kèm PLAN/BALANCE, giữ lại làm nhánh phụ theo AC6) — service này lấy Lot làm
/// entrypoint duy nhất, không tính PLAN/BALANCE.
/// </summary>
public interface ILotReportService
{
    /// <summary>AC1/AC2 — gợi ý (autocomplete) danh sách Lot khớp gần đúng <paramref name="search"/>. Rỗng nếu <paramref name="search"/> rỗng/toàn khoảng trắng.</summary>
    Task<IReadOnlyList<LotSearchItemDto>> SearchLotsAsync(string? search, CancellationToken cancellationToken = default);

    /// <summary>AC3/AC4/AC5 — chi tiết 1 Lot. Trả về <c>null</c> nếu <paramref name="lot"/> chưa từng có kế hoạch nào (AC2 "Không tìm thấy Lot").</summary>
    Task<LotSummaryDto?> GetLotSummaryAsync(string lot, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
