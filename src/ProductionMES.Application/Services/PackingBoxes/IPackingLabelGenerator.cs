namespace ProductionMES.Application.Services.PackingBoxes;

/// <summary>
/// Merge <see cref="PackingLabelData"/> vào file mẫu tem (template .xlsx, US-24) thành 1 file tem dán thùng hoàn
/// chỉnh, sẵn sàng gửi cho Station.Wpf tải xuống + in (US-25 AC4/AC13).
/// </summary>
public interface IPackingLabelGenerator
{
    /// <summary>
    /// Quy ước placeholder (quyết định thiết kế của US-25, không có trong SRS gốc — ghi chú lại theo CLAUDE.md):
    /// mọi cell dạng text trong template chứa 1 trong các token <c>{{Model}}</c>/<c>{{PartName}}</c>/
    /// <c>{{Manufacturer}}</c>/<c>{{PackingQuantity}}</c>/<c>{{GrossWeight}}</c>/<c>{{BoxNo}}</c>/<c>{{PackedAt}}</c>/
    /// <c>{{LineName}}</c>/<c>{{WorkStationName}}</c> sẽ được thay bằng giá trị tương ứng trong
    /// <paramref name="data"/> (khớp Admin đúng tên token khi thiết kế file mẫu ở US-24 AC4). Ném
    /// <see cref="Domain.Exceptions.BusinessRuleException"/> nếu <paramref name="templateContent"/> không phải
    /// file .xlsx hợp lệ (thiếu Excel — AC13).
    /// </summary>
    byte[] Generate(byte[] templateContent, PackingLabelData data);
}
