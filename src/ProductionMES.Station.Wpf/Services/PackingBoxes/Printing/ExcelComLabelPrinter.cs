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

    public Task PrintAsync(string filePath, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Excel COM bắt buộc chạy trên luồng STA — tách luồng riêng để không chặn UI thread của WPF.
        var thread = new Thread(() =>
        {
            try
            {
                PrintOnStaThread(filePath);
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

    private static void PrintOnStaThread(string filePath)
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

            // Không truyền ActivePrinter -> Excel in ra máy in mặc định của Windows (quyết định 09/10/2026).
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
}
