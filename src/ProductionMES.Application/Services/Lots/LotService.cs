using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Lots;
using ProductionMES.Application.Services.Reports;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Lots;

/// <summary>
/// Implementation <see cref="ILotService"/> (US-21a, viết lại hoàn toàn 19/08/2026; US-05 AC7-AC9).
/// </summary>
/// <remarks>
/// Soft-confirm (AC3/AC8) tái sử dụng <see cref="ILotReportService.GetLotSummaryAsync"/> (đã tính sẵn
/// <c>OkCount</c> theo từng dòng (Line, Công đoạn) của Lot — US-21 AC4/AC5) thay vì viết lại truy vấn Scan riêng.
/// <b>Không có chiều phụ thuộc ngược lại</b>: <see cref="LotReportService"/> đọc entity <see cref="Lot"/> trực
/// tiếp qua <see cref="IUnitOfWork"/> (KHÔNG qua <see cref="ILotService"/>) — tránh vòng phụ thuộc DI giữa 2
/// Service này.
/// </remarks>
public class LotService : ILotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILotReportService _lotReportService;

    public LotService(IUnitOfWork unitOfWork, ILotReportService lotReportService)
    {
        _unitOfWork = unitOfWork;
        _lotReportService = lotReportService;
    }

    public async Task<LotDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var lot = (await _unitOfWork.Repository<Lot>().FindAsync(l => l.Code == code, cancellationToken)).FirstOrDefault();
        return lot is null ? null : ToDto(lot);
    }

    public async Task<IReadOnlyDictionary<string, LotDto>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
    {
        var codeList = codes.Distinct().ToList();
        if (codeList.Count == 0)
        {
            return new Dictionary<string, LotDto>();
        }

        var lots = await _unitOfWork.Repository<Lot>().FindAsync(l => codeList.Contains(l.Code), cancellationToken);
        return lots.ToDictionary(l => l.Code, ToDto);
    }

    public async Task<bool> HasAnyProductionPlanAsync(string code, CancellationToken cancellationToken = default)
    {
        var plans = await _unitOfWork.Repository<ProductionPlan>().FindAsync(p => p.Lot == code, cancellationToken);
        return plans.Count > 0;
    }

    public async Task<LotDto> UpsertTotalQuantityAsync(
        string code, int totalQuantity, bool confirm, string? updatedByUserName, CancellationToken cancellationToken = default)
    {
        // AC3/AC8: soft-confirm khi giảm dưới số đã chạy thực tế — tái dùng LotReportService (đã có OkCount theo
        // từng (Line, Công đoạn), toàn bộ lịch sử — không giới hạn khoảng thời gian).
        var summary = await _lotReportService.GetLotSummaryAsync(code, fromUtc: null, toUtc: null, cancellationToken);
        if (summary is not null)
        {
            var violations = summary.Rows.Where(r => r.OkCount > totalQuantity).ToList();
            if (violations.Count > 0 && !confirm)
            {
                var detail = string.Join("; ", violations.Select(r => $"{r.LineName}/{r.StageName}: đã chạy OK {r.OkCount}"));
                throw new BusinessRuleException(
                    $"Tổng số lượng Lot mới ({totalQuantity}) NHỎ HƠN số lượng đã chạy OK thực tế tại: {detail}. " +
                    "Xác nhận thay đổi bằng cách gửi lại yêu cầu với Confirm = true.");
            }
        }

        var repository = _unitOfWork.Repository<Lot>();
        var existing = (await repository.FindAsync(l => l.Code == code, cancellationToken)).FirstOrDefault();
        // Đổi ý 19/08/2026: KHÔNG dùng UtcNow — lưu đúng giờ local hệ thống lúc cập nhật, không quy đổi (xem
        // API-Conventions.md mục 10).
        var now = DateTime.Now;

        if (existing is null)
        {
            existing = new Lot
            {
                Code = code,
                TotalQuantity = totalQuantity,
                UpdatedAtUtc = now,
                UpdatedByUserName = updatedByUserName,
            };
            await repository.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.TotalQuantity = totalQuantity;
            existing.UpdatedAtUtc = now;
            existing.UpdatedByUserName = updatedByUserName;
            repository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(existing);
    }

    private static LotDto ToDto(Lot lot) => new()
    {
        Code = lot.Code,
        TotalQuantity = lot.TotalQuantity,
        UpdatedAtUtc = lot.UpdatedAtUtc,
        UpdatedByUserName = lot.UpdatedByUserName,
    };
}
