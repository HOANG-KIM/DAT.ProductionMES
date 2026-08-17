using System.Globalization;
using System.Windows.Data;
using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Converters;

/// <summary>
/// Tra tên Line theo Id (US-05 AC1c, US-05b bug hardcode "Line 1") — dùng <see cref="IMultiValueConverter"/>
/// thay vì <see cref="IValueConverter"/>+<c>ConverterParameter</c> vì danh mục Line (<c>AllLines</c>) nạp bất
/// đồng bộ sau khi trang mở: <c>ConverterParameter</c> không phải dependency property nên không tự cập nhật khi
/// ViewModel thay đổi tham chiếu collection, còn <see cref="MultiBinding"/> thì có.
/// </summary>
public class LineIdToNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int lineId || values[1] is not IEnumerable<LineDto> lines)
        {
            return string.Empty;
        }

        return lines.FirstOrDefault(l => l.Id == lineId)?.Name ?? $"#{lineId}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
