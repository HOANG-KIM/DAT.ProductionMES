namespace ProductionMES.Station.Wpf.Services.PackingBoxes;

/// <summary>Tải + in tem thùng tại trạm (US-25 AC4/AC13) — tải file tem (đã merge dữ liệu ở server) rồi gửi cho máy in mặc định của trạm.</summary>
public interface IPackingLabelPrintService
{
    /// <summary>
    /// Tải file tem của <paramref name="boxId"/> rồi gửi lệnh in. Ném <see cref="PackingLabelPrintException"/> khi
    /// CHÍNH lệnh gọi in thất bại (tải file lỗi — thiếu template/Excel ở server, hoặc không có ứng dụng/máy in xử
    /// lý được lệnh in .xlsx tại trạm) — AC13. KHÔNG ném lỗi cho trường hợp máy in vật lý kẹt/hết giấy (không phát
    /// hiện được ở tầng code — lệnh in đã gửi thành công tới hệ điều hành).
    /// </summary>
    Task PrintAsync(int boxId, CancellationToken cancellationToken = default);
}
