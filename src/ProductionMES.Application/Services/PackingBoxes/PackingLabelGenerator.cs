using ClosedXML.Excel;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.PackingBoxes;

/// <inheritdoc cref="IPackingLabelGenerator"/>
public class PackingLabelGenerator : IPackingLabelGenerator
{
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

            foreach (var sheet in workbook.Worksheets)
            {
                foreach (var cell in sheet.CellsUsed(c => c.DataType == XLDataType.Text))
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

    private static IReadOnlyList<(string Token, string Value)> BuildReplacements(PackingLabelData data) => new[]
    {
        ("{{Model}}", data.Model),
        ("{{PartName}}", data.PartName),
        ("{{Manufacturer}}", data.Manufacturer ?? string.Empty),
        ("{{PackingQuantity}}", data.PackingQuantity.ToString()),
        ("{{GrossWeight}}", data.GrossWeight.HasValue ? data.GrossWeight.Value.ToString("0.##") : string.Empty),
        ("{{BoxNo}}", data.BoxNo.ToString()),
        ("{{PackedAt}}", data.PackedAtLocal.ToString("dd/MM/yyyy HH:mm")),
        ("{{LineName}}", data.LineName),
        ("{{WorkStationName}}", data.WorkStationName),
    };
}
