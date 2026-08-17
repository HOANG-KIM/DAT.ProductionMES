using ProductionMES.Application.DTOs.AndonBoard;

namespace ProductionMES.Application.Services.AndonBoard;

/// <summary>Service cung cấp dữ liệu bảng PLAN/ACTUAL/BALANCE theo mốc giờ tại màn hình trạm (US-09/FR-09/FR-09a).</summary>
public interface IAndonBoardService
{
    /// <summary>
    /// Lấy dữ liệu bảng cho đúng 1 trạm. Không ném lỗi khi (Line, Công đoạn) của trạm chưa có kế hoạch nào đang
    /// Running (khác <c>ScanService</c>) — trả về <see cref="AndonBoardDto.HasActivePlan"/> = false, vì đây là
    /// endpoint chỉ HIỂN THỊ, không phải hành động ghi nhận scan.
    /// </summary>
    /// <param name="workStationId">
    /// Id trạm THẬT lấy từ danh tính đã xác thực (claim), không phải từ request — cùng nguyên tắc với
    /// <c>IScanService.CreateAsync</c> (ADR-005).
    /// </param>
    Task<AndonBoardDto> GetForWorkStationAsync(int workStationId, CancellationToken cancellationToken = default);
}
