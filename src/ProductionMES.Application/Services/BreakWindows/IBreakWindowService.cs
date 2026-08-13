using ProductionMES.Application.DTOs.BreakWindows;

namespace ProductionMES.Application.Services.BreakWindows;

/// <summary>Service quản lý khung giờ nghỉ theo Line (US-01a/FR-01/FR-09a).</summary>
public interface IBreakWindowService
{
    /// <summary>Lấy toàn bộ khung giờ nghỉ đã cấu hình cho 1 Line (AC2 — có thể rỗng, AC4).</summary>
    Task<IReadOnlyList<BreakWindowDto>> GetByLineAsync(int lineId, CancellationToken cancellationToken = default);

    /// <summary>Thêm 1 khung giờ nghỉ cho Line — từ chối nếu không hợp lệ hoặc chồng lấn khung đã có (AC1/AC5).</summary>
    Task<BreakWindowDto> CreateAsync(int lineId, CreateBreakWindowRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sửa 1 khung giờ nghỉ đã tồn tại — từ chối nếu không hợp lệ hoặc chồng lấn khung khác (AC3/AC5).</summary>
    Task<BreakWindowDto> UpdateAsync(int lineId, int id, UpdateBreakWindowRequest request, CancellationToken cancellationToken = default);

    /// <summary>Xóa 1 khung giờ nghỉ (AC3) — bản ghi cấu hình thuần túy, không có ý nghĩa lịch sử độc lập nên xóa cứng.</summary>
    Task DeleteAsync(int lineId, int id, CancellationToken cancellationToken = default);
}
