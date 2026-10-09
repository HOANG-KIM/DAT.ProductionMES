using System.Runtime.InteropServices;

namespace ProductionMES.Station.Wpf.Services.PackingBoxes.Printing;

/// <summary>
/// In qua Microsoft Excel bằng COM late-binding (<c>dynamic</c>, không reference Interop assembly — không phụ thuộc
/// phiên bản Office cài tại trạm). Cách làm lấy theo phần mềm in tem cũ PrintLabel (WinForms) đã chạy thực tế ở
/// xưởng: mở Excel ẩn trên 1 luồng STA riêng, <c>PrintOut()</c>, đóng workbook + thoát Excel, giải phóng COM.
/// </summary>
public class ExcelComLabelPrinter : ILabelPrinter
{
    private const string ExcelProgId = "Excel.Application";

    public string DisplayName => "Microsoft Excel";

    public bool IsAvailable() => Type.GetTypeFromProgID(ExcelProgId) != null;

    public Task PrintAsync(string filePath, string printerName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Excel COM bắt buộc chạy trên luồng STA — tách luồng riêng để không chặn UI thread của WPF.
        var thread = new Thread(() =>
        {
            try
            {
                PrintOnStaThread(filePath, printerName);
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "ExcelLabelPrint",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        // Quá thời gian thì báo lỗi cho Operator; luồng STA vẫn tự chạy nốt và dọn Excel trong finally của nó.
        return completion.Task.WaitAsync(timeout, cancellationToken);
    }

    private static void PrintOnStaThread(string filePath, string printerName)
    {
        var excelType = Type.GetTypeFromProgID(ExcelProgId)
            ?? throw new InvalidOperationException("Microsoft Excel chưa được cài đặt trên máy này.");

        dynamic? excel = null;
        dynamic? workbook = null;
        try
        {
            excel = Activator.CreateInstance(excelType)!;
            excel.Visible = false;
            excel.DisplayAlerts = false;

            workbook = excel.Workbooks.Open(filePath, ReadOnly: true);

            if (!string.IsNullOrWhiteSpace(printerName))
            {
                SetActivePrinter(excel, printerName.Trim());
            }

            workbook.PrintOut();
        }
        finally
        {
            if (workbook != null)
            {
                try { workbook.Close(false); } catch (COMException) { }
                Marshal.FinalReleaseComObject(workbook);
            }

            if (excel != null)
            {
                try { excel.Quit(); } catch (COMException) { }
                Marshal.FinalReleaseComObject(excel);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// Excel chỉ nhận tên máy in kèm cổng dạng <c>"Tên máy in on Ne01:"</c> (cổng NeXX khác nhau theo từng máy) — thử
    /// tên trần trước, rồi lần lượt Ne00..Ne99 cho tới khi Excel chấp nhận.
    /// </summary>
    private static void SetActivePrinter(dynamic excel, string printerName)
    {
        try
        {
            excel.ActivePrinter = printerName;
            return;
        }
        catch (COMException)
        {
        }

        for (var port = 0; port < 100; port++)
        {
            try
            {
                excel.ActivePrinter = $"{printerName} on Ne{port:00}:";
                return;
            }
            catch (COMException)
            {
            }
        }

        throw new InvalidOperationException($"Không tìm thấy máy in \"{printerName}\" — kiểm tra lại LabelPrint:PrinterName trong appsettings.json của trạm.");
    }
}
