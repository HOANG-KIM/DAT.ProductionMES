namespace ProductionMES.Station.Wpf.Services.PackingBoxes.Printing;

/// <summary>1 cách in file tem thùng .xlsx đã merge sẵn dữ liệu (US-28) — Excel COM hoặc LibreOffice.</summary>
public interface ILabelPrinter
{
    /// <summary>Tên hiển thị trong thông báo lỗi (vd "Microsoft Excel").</summary>
    string DisplayName { get; }

    /// <summary>Trạm có cài phần mềm tương ứng không — dùng cho chế độ <c>Auto</c> chọn cách in.</summary>
    bool IsAvailable();

    /// <summary>
    /// In <paramref name="filePath"/> ra máy in mặc định của Windows, CHỜ tới khi lệnh in
    /// đã gửi xong cho Windows. Ném exception bất kỳ khi CHÍNH lệnh gọi in thất bại — caller bọc lại thành
    /// <see cref="PackingLabelPrintException"/>.
    /// </summary>
    Task PrintAsync(string filePath, TimeSpan timeout, CancellationToken cancellationToken);
}
