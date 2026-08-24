using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.PackingBoxes;

/// <inheritdoc cref="IPackingLabelPrintService"/>
/// <remarks>
/// Quyết định thiết kế của US-25 (không có trong SRS gốc, ghi chú lại theo CLAUDE.md): dùng
/// <see cref="Process.Start(ProcessStartInfo)"/> với <c>Verb = "print"</c> (ShellExecute) trên file .xlsx đã tải
/// về — dựa vào ứng dụng mặc định của Windows xử lý lệnh in .xlsx (thường là Excel/LibreOffice Calc đã cài tại
/// trạm) thay vì tự vẽ trang in bằng thư viện in ấn riêng. <see cref="Win32Exception"/>/<see cref="InvalidOperationException"/>
/// khi gọi <c>Process.Start</c> LÀ lỗi "CHÍNH lệnh gọi in thất bại" (AC13 — thường do trạm chưa cài ứng dụng mở
/// được .xlsx, hoặc chưa cấu hình máy in mặc định) — phân biệt với lỗi vật lý (kẹt/hết giấy) xảy ra SAU khi lệnh
/// in đã được ứng dụng đó gửi thành công tới máy in, hoàn toàn không phát hiện được ở tầng code này.
/// </remarks>
public class PackingLabelPrintService : IPackingLabelPrintService
{
    private readonly IPackingBoxApiClient _apiClient;

    public PackingLabelPrintService(IPackingBoxApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task PrintAsync(int boxId, CancellationToken cancellationToken = default)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"tem-thung-{boxId}-{Guid.NewGuid():N}.xlsx");

        try
        {
            await _apiClient.DownloadLabelAsync(boxId, tempFilePath, cancellationToken);
        }
        catch (ApiException ex)
        {
            // AC13 "thiếu template/Excel" — server không tạo được file tem (thường do Model chưa có mẫu tem).
            throw new PackingLabelPrintException($"Không tải được tem thùng để in: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new PackingLabelPrintException($"Không kết nối được server để tải tem thùng: {ex.Message}", ex);
        }

        try
        {
            var startInfo = new ProcessStartInfo(tempFilePath)
            {
                UseShellExecute = true,
                Verb = "print",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            using var process = Process.Start(startInfo);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // AC13 "thiếu máy in mặc định"/không có ứng dụng xử lý được lệnh in .xlsx tại trạm — CHÍNH lệnh gọi in
            // thất bại (khác lỗi vật lý kẹt/hết giấy, xảy ra sau bước này, không bắt được ở đây).
            throw new PackingLabelPrintException(
                "Không thể gửi lệnh in tem thùng — trạm này chưa cài ứng dụng mở/in file .xlsx hoặc chưa có máy in mặc định.", ex);
        }
    }
}
