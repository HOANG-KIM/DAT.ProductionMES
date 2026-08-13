using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.BreakWindows;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.BreakWindows;

/// <summary>
/// Implementation IBreakWindowService (US-01a/FR-01/FR-09a). Kiểm tra chồng lấn (AC5) thực hiện ở đây (không
/// phải FluentValidation validator) vì cần truy vấn toàn bộ khung giờ nghỉ hiện có của Line — theo đúng
/// convention business rule cần dữ liệu đã áp dụng cho <c>ProductionPlanStageService</c> (ném
/// <see cref="BusinessRuleException"/>, không dựa vào ràng buộc DB).
/// </summary>
public class BreakWindowService : IBreakWindowService
{
    private readonly IUnitOfWork _unitOfWork;

    public BreakWindowService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BreakWindowDto>> GetByLineAsync(int lineId, CancellationToken cancellationToken = default)
    {
        await EnsureLineExistsAsync(lineId, cancellationToken);

        var items = await _unitOfWork.Repository<BreakWindow>().FindAsync(x => x.LineId == lineId, cancellationToken);
        return items.OrderBy(x => x.StartTime).Select(ToDto).ToList();
    }

    public async Task<BreakWindowDto> CreateAsync(int lineId, CreateBreakWindowRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureLineExistsAsync(lineId, cancellationToken);

        var repository = _unitOfWork.Repository<BreakWindow>();
        var existingItems = await repository.FindAsync(x => x.LineId == lineId, cancellationToken);

        EnsureNoOverlap(existingItems, request.StartTime, request.EndTime, excludingId: null);

        var breakWindow = new BreakWindow
        {
            LineId = lineId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Note = request.Note,
        };

        await repository.AddAsync(breakWindow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(breakWindow);
    }

    public async Task<BreakWindowDto> UpdateAsync(int lineId, int id, UpdateBreakWindowRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureLineExistsAsync(lineId, cancellationToken);

        var repository = _unitOfWork.Repository<BreakWindow>();
        var breakWindow = await repository.GetByIdAsync(id, cancellationToken);
        if (breakWindow is null || breakWindow.LineId != lineId)
        {
            throw new EntityNotFoundException($"Không tìm thấy khung giờ nghỉ với Id = {id} thuộc Line = {lineId}.");
        }

        var existingItems = await repository.FindAsync(x => x.LineId == lineId, cancellationToken);
        EnsureNoOverlap(existingItems, request.StartTime, request.EndTime, excludingId: id);

        // AC3: sửa có hiệu lực ngay cho lần tính tiếp theo, không ảnh hưởng số liệu lịch sử đã tính trước đó
        // (số liệu lịch sử là dữ liệu tính toán đầu ra ở nơi khác, không nằm trong phạm vi entity này).
        breakWindow.StartTime = request.StartTime;
        breakWindow.EndTime = request.EndTime;
        breakWindow.Note = request.Note;

        repository.Update(breakWindow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(breakWindow);
    }

    public async Task DeleteAsync(int lineId, int id, CancellationToken cancellationToken = default)
    {
        await EnsureLineExistsAsync(lineId, cancellationToken);

        var repository = _unitOfWork.Repository<BreakWindow>();
        var breakWindow = await repository.GetByIdAsync(id, cancellationToken);
        if (breakWindow is null || breakWindow.LineId != lineId)
        {
            throw new EntityNotFoundException($"Không tìm thấy khung giờ nghỉ với Id = {id} thuộc Line = {lineId}.");
        }

        repository.Remove(breakWindow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLineExistsAsync(int lineId, CancellationToken cancellationToken)
    {
        var line = await _unitOfWork.Repository<Line>().GetByIdAsync(lineId, cancellationToken);
        if (line is null)
        {
            throw new EntityNotFoundException($"Không tìm thấy Line với Id = {lineId}.");
        }
    }

    /// <summary>
    /// AC5: từ chối khi khung giờ nghỉ mới/sửa chồng lấn 1 khung khác đã có của cùng Line. 2 khoảng [s1,e1) và
    /// [s2,e2) chồng lấn khi s1 &lt; e2 &amp;&amp; s2 &lt; e1 (đã đảm bảo EndTime &gt; StartTime ở validator nên
    /// không có khoảng "qua đêm" cần xử lý riêng).
    /// </summary>
    private static void EnsureNoOverlap(IEnumerable<BreakWindow> existingItems, TimeOnly startTime, TimeOnly endTime, int? excludingId)
    {
        var overlapping = existingItems
            .Where(x => excludingId is null || x.Id != excludingId.Value)
            .Any(x => startTime < x.EndTime && x.StartTime < endTime);

        if (overlapping)
        {
            throw new BusinessRuleException(
                $"Khung giờ nghỉ {startTime:HH\\:mm}–{endTime:HH\\:mm} chồng lấn với 1 khung giờ nghỉ khác đã cấu hình cho Line này.");
        }
    }

    private static BreakWindowDto ToDto(BreakWindow breakWindow) => new()
    {
        Id = breakWindow.Id,
        LineId = breakWindow.LineId,
        StartTime = breakWindow.StartTime,
        EndTime = breakWindow.EndTime,
        Note = breakWindow.Note,
    };
}
