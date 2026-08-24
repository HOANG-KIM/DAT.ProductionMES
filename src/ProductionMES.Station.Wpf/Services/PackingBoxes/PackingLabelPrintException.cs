namespace ProductionMES.Station.Wpf.Services.PackingBoxes;

/// <summary>Lỗi CHÍNH lệnh gọi in thất bại (US-25 AC13) — KHÁC lỗi vật lý máy in (kẹt/hết giấy), không phát hiện được ở tầng code.</summary>
public class PackingLabelPrintException : Exception
{
    public PackingLabelPrintException(string message) : base(message)
    {
    }

    public PackingLabelPrintException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
