namespace ProductionMES.Application.Services.PackingBoxes;

/// <summary>1 token hợp lệ dùng trong file mẫu tem in (template .xlsx) — <see cref="Token"/> khớp Ordinal, <see cref="Description"/> tiếng Việt để hiển thị tham chiếu trên UI (US-24/AC10).</summary>
public record PackingLabelTokenInfo(string Token, string Description);

/// <summary>
/// Nguồn khai báo DUY NHẤT 9 token hợp lệ dùng trong file mẫu tem in (template .xlsx, US-24 AC4/AC10, US-25
/// AC4) — <see cref="PackingLabelGenerator"/> (merge dữ liệu thật lúc in, US-25) và
/// <c>PackingModelConfigService</c> (quét cảnh báo token sai lúc upload, AC10) đều tham chiếu list này thay vì
/// lặp lại 9 chuỗi token ở nhiều nơi. Cũng được <c>PackingModelConfigsController</c> trả ra API cho cả
/// web-admin lẫn Station.Wpf hiển thị tham chiếu (AC10 — "không phải tự đoán hoặc tra code").
/// </summary>
public static class PackingLabelTokens
{
    public static readonly IReadOnlyList<PackingLabelTokenInfo> All = new[]
    {
        new PackingLabelTokenInfo("{{Model}}", "Model sản phẩm"),
        new PackingLabelTokenInfo("{{PartName}}", "Tên sản phẩm"),
        new PackingLabelTokenInfo("{{Manufacturer}}", "Nhà sản xuất"),
        new PackingLabelTokenInfo("{{PackingQuantity}}", "Quy cách đóng gói (số lượng sản phẩm/thùng)"),
        new PackingLabelTokenInfo("{{GrossWeight}}", "Khối lượng"),
        new PackingLabelTokenInfo("{{BoxNo}}", "Số thứ tự thùng"),
        new PackingLabelTokenInfo("{{PackedAt}}", "Ngày giờ đóng thùng"),
        new PackingLabelTokenInfo("{{LineName}}", "Tên Line sản xuất"),
        new PackingLabelTokenInfo("{{WorkStationName}}", "Tên trạm làm việc"),
    };

    /// <summary>So khớp <c>StringComparer.Ordinal</c> — phân biệt hoa/thường tuyệt đối, cùng quy ước với <see cref="PackingLabelGenerator"/>.</summary>
    public static readonly IReadOnlySet<string> ValidTokens = All.Select(t => t.Token).ToHashSet(StringComparer.Ordinal);
}
