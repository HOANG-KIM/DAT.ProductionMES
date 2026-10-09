using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using ProductionMES.Domain.Exceptions;
using QRCoder;

namespace ProductionMES.Application.Services.PackingBoxes;

/// <inheritdoc cref="IPackingLabelGenerator"/>
public class PackingLabelGenerator : IPackingLabelGenerator
{
    /// <summary>Lề (pixel) giữa ảnh QR và mép vùng ô chứa token — tránh ảnh đè lên đường kẻ viền ô khi in.</summary>
    private const int QrPaddingPixels = 4;

    /// <summary>Cạnh tối thiểu (pixel) của ảnh QR khi vùng ô chứa token quá nhỏ — dưới mức này máy quét khó đọc.</summary>
    private const int QrMinSizePixels = 60;

    public byte[] Generate(byte[] templateContent, PackingLabelData data)
    {
        using var templateStream = new MemoryStream(templateContent);
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(templateStream);
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            // AC13: "thiếu Excel" — template hỏng/không phải file .xlsx hợp lệ -> đây LÀ lỗi của chính lệnh gọi in
            // (không phải lỗi vật lý máy in), phải chặn/báo lỗi rõ ràng.
            throw new BusinessRuleException("File mẫu tem hiện tại không phải file .xlsx hợp lệ — vui lòng tải lên lại mẫu tem cho Model này.");
        }

        using (workbook)
        {
            var replacements = BuildReplacements(data);
            byte[]? qrPng = null;

            foreach (var sheet in workbook.Worksheets)
            {
                // ToList(): chèn ảnh QR trong lúc duyệt không làm thay đổi tập ô, nhưng tách riêng cho chắc chắn.
                foreach (var cell in sheet.CellsUsed(c => c.DataType == XLDataType.Text).ToList())
                {
                    var text = cell.GetString();
                    if (text.Length == 0)
                    {
                        continue;
                    }

                    var replaced = text;
                    foreach (var (token, value) in replacements)
                    {
                        replaced = replaced.Replace(token, value, StringComparison.Ordinal);
                    }

                    // US-28: token ảnh QR — xoá token khỏi ô (giữ phần chữ khác nếu Admin gõ kèm) rồi chèn ảnh.
                    if (replaced.Contains(PackingLabelTokens.QrCodeToken, StringComparison.Ordinal))
                    {
                        replaced = replaced.Replace(PackingLabelTokens.QrCodeToken, string.Empty, StringComparison.Ordinal);
                        qrPng ??= BuildQrPng(BuildQrContent(data));
                        InsertQrImage(sheet, cell, qrPng);
                    }

                    if (!ReferenceEquals(replaced, text))
                    {
                        cell.SetValue(replaced);
                    }
                }
            }

            using var outputStream = new MemoryStream();
            workbook.SaveAs(outputStream);
            return outputStream.ToArray();
        }
    }

    /// <summary>
    /// Nội dung mã hoá trong QR (US-28) — giữ đúng định dạng tem cũ của phần mềm PrintLabel (WinForms) để máy quét/hệ
    /// thống kho đang đọc tem cũ không phải đổi: <c>Model\Lot\BoxNo\NhânViên\SốLượng\yyyyMMdd</c>.
    /// </summary>
    public static string BuildQrContent(PackingLabelData data) =>
        string.Join('\\',
            data.Model,
            data.Lot,
            data.BoxNo.ToString(),
            data.OperatorNames,
            data.PackingQuantity.ToString(),
            data.PackedAtLocal.ToString("yyyyMMdd"));

    private static byte[] BuildQrPng(string content)
    {
        using var generator = new QRCodeGenerator();
        // ECC M + UTF-8 tường minh: OperatorNames có thể chứa tiếng Việt có dấu.
        using var qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M, forceUtf8: true);
        // 10 pixel/module: ảnh gốc đủ nét, Excel/LibreOffice co lại theo kích thước vùng ô khi in.
        return new PngByteQRCode(qrData).GetGraphic(10);
    }

    /// <summary>
    /// Chèn ảnh QR vuông, căn giữa trong ô chứa token — nếu ô thuộc vùng gộp (merged) thì lấy cả vùng gộp, để Admin
    /// điều khiển kích thước QR chỉ bằng cách gộp ô/kéo cột-hàng trong Excel lúc thiết kế template.
    /// </summary>
    private static void InsertQrImage(IXLWorksheet sheet, IXLCell cell, byte[] qrPng)
    {
        var range = cell.IsMerged() ? cell.MergedRange() : cell.AsRange();

        var columnWidths = range.Columns().Select(c => (c.ColumnNumber(), ColumnWidthToPixels(sheet.Column(c.ColumnNumber()).Width))).ToList();
        var rowHeights = range.Rows().Select(r => (r.RowNumber(), PointsToPixels(sheet.Row(r.RowNumber()).Height))).ToList();
        var widthPx = columnWidths.Sum(c => c.Item2);
        var heightPx = rowHeights.Sum(r => r.Item2);

        var size = Math.Max(QrMinSizePixels, Math.Min(widthPx, heightPx) - 2 * QrPaddingPixels);

        // Offset căn giữa có thể vượt quá 1 hàng/cột (vd vùng gộp A1:A6 cao gấp nhiều lần rộng) — Excel tự cắt offset
        // lớn hơn kích thước ô neo, làm ảnh lệch lên trên. Nên dời ô neo tới đúng hàng/cột chứa điểm bắt đầu.
        var (anchorColumn, offsetX) = LocateAnchor(columnWidths, Math.Max(0, (widthPx - size) / 2));
        var (anchorRow, offsetY) = LocateAnchor(rowHeights, Math.Max(0, (heightPx - size) / 2));

        using var imageStream = new MemoryStream(qrPng);
        sheet.AddPicture(imageStream, XLPictureFormat.Png)
            .MoveTo(sheet.Cell(anchorRow, anchorColumn), offsetX, offsetY)
            .WithSize(size, size);
    }

    /// <summary>Từ offset tính trên cả vùng, tìm hàng/cột chứa điểm đó và offset còn lại bên trong hàng/cột đó.</summary>
    private static (int Index, int Offset) LocateAnchor(IReadOnlyList<(int Index, int SizePx)> segments, int offset)
    {
        foreach (var (index, sizePx) in segments)
        {
            if (offset < sizePx)
            {
                return (index, offset);
            }

            offset -= sizePx;
        }

        return (segments[^1].Index, 0);
    }

    /// <summary>Quy đổi độ rộng cột Excel (đơn vị ký tự, font mặc định Calibri 11) sang pixel ở 96 DPI — công thức xấp xỉ chuẩn của Excel.</summary>
    private static int ColumnWidthToPixels(double width) => (int)Math.Round(width * 7 + 5);

    private static int PointsToPixels(double points) => (int)Math.Round(points * 96 / 72);

    /// <summary>
    /// Giá trị thật gắn với từng token chữ — KHÔNG khai báo lại danh sách token ở đây (nguồn duy nhất là
    /// <see cref="PackingLabelTokens.All"/>), chỉ ánh xạ token -> giá trị để tránh 2 nơi lệch nhau nếu sau này
    /// thêm/bớt token. Token ảnh <see cref="PackingLabelTokens.QrCodeToken"/> xử lý riêng, không nằm trong ánh xạ này.
    /// </summary>
    private static IReadOnlyList<(string Token, string Value)> BuildReplacements(PackingLabelData data)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{Model}}"] = data.Model,
            ["{{PartName}}"] = data.PartName,
            ["{{Manufacturer}}"] = data.Manufacturer ?? string.Empty,
            ["{{PackingQuantity}}"] = data.PackingQuantity.ToString(),
            ["{{GrossWeight}}"] = data.GrossWeight.HasValue ? data.GrossWeight.Value.ToString("0.##") : string.Empty,
            ["{{BoxNo}}"] = data.BoxNo.ToString(),
            ["{{PackedAt}}"] = data.PackedAtLocal.ToString("dd/MM/yyyy HH:mm"),
            ["{{PackedDate}}"] = data.PackedAtLocal.ToString("dd/MM/yyyy"),
            ["{{LineName}}"] = data.LineName,
            ["{{WorkStationName}}"] = data.WorkStationName,
            ["{{Customer}}"] = data.Customer,
            ["{{Lot}}"] = data.Lot,
            ["{{OperatorNames}}"] = data.OperatorNames,
        };

        return PackingLabelTokens.All
            .Where(t => t.Token != PackingLabelTokens.QrCodeToken)
            .Select(t => (t.Token, values[t.Token]))
            .ToList();
    }
}
