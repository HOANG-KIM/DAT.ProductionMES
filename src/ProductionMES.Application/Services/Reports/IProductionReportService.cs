using ProductionMES.Application.DTOs.Reports;

namespace ProductionMES.Application.Services.Reports;

/// <summary>Service cung cấp báo cáo tổng hợp ACTUAL/NG/PLAN/BALANCE theo Line/Lot/công đoạn (US-21/FR-21).</summary>
public interface IProductionReportService
{
    /// <summary>
    /// Lấy báo cáo tổng hợp — mỗi dòng đại diện 1 bộ (Line, Lot, Công đoạn) (AC4) cho MỌI cặp (Line, Công đoạn)
    /// đang có trạm làm việc hoạt động (hoặc chỉ 1 Line/Công đoạn nếu <see cref="ProductionReportQuery.LineId"/>/
    /// <see cref="ProductionReportQuery.StageId"/> được truyền). Không ném lỗi khi 1 cặp chưa/không còn kế hoạch
    /// tham chiếu — trả về <see cref="ProductionReportRowDto.HasPlanData"/> = false cho dòng đó (endpoint chỉ
    /// hiển thị, giống <c>IAndonBoardService</c>), TRỪ khi đang áp dụng bộ lọc Model/Customer/Revision/Lot (AC6)
    /// và không còn gì khớp — khi đó cặp bị loại hẳn khỏi kết quả, xem remarks tại <c>ProductionReportService</c>.
    /// </summary>
    Task<ProductionReportDto> GetReportAsync(ProductionReportQuery query, CancellationToken cancellationToken = default);
}
