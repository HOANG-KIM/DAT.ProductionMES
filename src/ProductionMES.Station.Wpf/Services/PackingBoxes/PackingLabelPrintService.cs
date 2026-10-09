using System.IO;
using System.Net.Http;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.PackingBoxes.Printing;

namespace ProductionMES.Station.Wpf.Services.PackingBoxes;

/// <inheritdoc cref="IPackingLabelPrintService"/>
/// <remarks>
/// US-28 (thay cách in ShellExecute <c>Verb = "print"</c> của US-25 — phụ thuộc ứng dụng mặc định của Windows, không
/// chọn được máy in, không biết in xong chưa): in qua Excel COM hoặc LibreOffice headless theo
/// <see cref="LabelPrintOptions.Engine"/> cấu hình cục bộ tại trạm. Mọi lỗi của CHÍNH lệnh gọi in (thiếu template ở
/// server, thiếu Excel/LibreOffice, sai tên máy in, quá thời gian) đều bọc thành <see cref="PackingLabelPrintException"/>
/// (AC13) — lỗi vật lý kẹt/hết giấy xảy ra sau khi Windows đã nhận lệnh in, vẫn không phát hiện được ở tầng này.
/// </remarks>
public class PackingLabelPrintService : IPackingLabelPrintService
{
    private readonly IPackingBoxApiClient _apiClient;
    private readonly LabelPrintOptions _options;
    private readonly ExcelComLabelPrinter _excelPrinter;
    private readonly LibreOfficeLabelPrinter _libreOfficePrinter;

    /// <summary>
    /// In tuần tự từng tem — "In lại" có thể bấm trong lúc tem tự động còn đang in; LibreOffice dùng chung 1 profile
    /// nên 2 tiến trình chạy song song sẽ tranh nhau khoá profile.
    /// </summary>
    private readonly SemaphoreSlim _printLock = new(1, 1);

    public PackingLabelPrintService(
        IPackingBoxApiClient apiClient, StationOptions stationOptions,
        ExcelComLabelPrinter excelPrinter, LibreOfficeLabelPrinter libreOfficePrinter)
    {
        _apiClient = apiClient;
        _options = stationOptions.LabelPrint;
        _excelPrinter = excelPrinter;
        _libreOfficePrinter = libreOfficePrinter;
    }

    public async Task PrintAsync(int boxId, CancellationToken cancellationToken = default)
    {
        var printer = ResolvePrinter();

        await _printLock.WaitAsync(cancellationToken);
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"tem-thung-{boxId}-{Guid.NewGuid():N}.xlsx");
        try
        {
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
                await printer.PrintAsync(
                    tempFilePath, _options.PrinterName, TimeSpan.FromSeconds(Math.Max(10, _options.TimeoutSeconds)), cancellationToken);
            }
            catch (TimeoutException ex)
            {
                throw new PackingLabelPrintException(
                    $"{printer.DisplayName} không in xong sau {_options.TimeoutSeconds} giây — kiểm tra máy in rồi bấm \"In lại\".", ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new PackingLabelPrintException($"{printer.DisplayName} báo lỗi khi in: {ex.Message}", ex);
            }
        }
        finally
        {
            _printLock.Release();
            TryDeleteFile(tempFilePath);
        }
    }

    private ILabelPrinter ResolvePrinter()
    {
        switch (_options.Engine)
        {
            case LabelPrintEngine.Excel:
                return _excelPrinter.IsAvailable()
                    ? _excelPrinter
                    : throw new PackingLabelPrintException("Trạm cấu hình in bằng Excel (LabelPrint:Engine = Excel) nhưng máy này chưa cài Microsoft Excel.");

            case LabelPrintEngine.LibreOffice:
                return _libreOfficePrinter.IsAvailable()
                    ? _libreOfficePrinter
                    : throw new PackingLabelPrintException("Trạm cấu hình in bằng LibreOffice (LabelPrint:Engine = LibreOffice) nhưng không tìm thấy soffice.exe.");

            default:
                if (_excelPrinter.IsAvailable())
                {
                    return _excelPrinter;
                }

                return _libreOfficePrinter.IsAvailable()
                    ? _libreOfficePrinter
                    : throw new PackingLabelPrintException("Máy này chưa cài Microsoft Excel hoặc LibreOffice — cần cài 1 trong 2 để in tem thùng.");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Excel/LibreOffice có thể còn giữ file vài giây sau khi in xong — để lại file tạm, không ảnh hưởng nghiệp vụ.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
