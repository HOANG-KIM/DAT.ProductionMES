using ClosedXML.Excel;
using ProductionMES.Application.Services.PackingBoxes;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Test merge dữ liệu vào template tem thùng (US-25 AC4, US-28) — dùng file .xlsx THẬT tạo bằng ClosedXML (không
/// mock), vì đây chính là phần logic đọc/ghi Excel cần kiểm chứng.
/// </summary>
public class PackingLabelGeneratorTests
{
    private readonly PackingLabelGenerator _sut = new();

    private static PackingLabelData SampleData() => new()
    {
        Model = "ABC-123",
        PartName = "Dây điện",
        Manufacturer = "DAT",
        PackingQuantity = 50,
        GrossWeight = 12.5m,
        BoxNo = 7,
        PackedAtLocal = new DateTime(2026, 10, 9, 14, 30, 0),
        LineName = "Line 1",
        WorkStationName = "Trạm đóng thùng",
        Customer = "SELIM",
        Lot = "LOT-01",
        OperatorNames = "Nguyễn Văn A",
    };

    private static byte[] BuildTemplate(Action<IXLWorksheet> configure)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Tem");
        configure(sheet);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static XLWorkbook Open(byte[] content) => new(new MemoryStream(content));

    // US-28 — Token chữ mới (Customer/Lot/OperatorNames/PackedDate) được thay đúng giá trị, kể cả khi nằm giữa chuỗi.
    [Fact]
    public void Generate_TokenChuMoi_ThayDungGiaTri()
    {
        var template = BuildTemplate(s =>
        {
            s.Cell("A1").Value = "{{Customer}}";
            s.Cell("A2").Value = "Lot: {{Lot}}";
            s.Cell("A3").Value = "NV cân: {{OperatorNames}}";
            s.Cell("A4").Value = "{{PackedDate}}";
            s.Cell("A5").Value = "{{PackingQuantity}} EA/BOX ({{GrossWeight}} Kg)";
        });

        using var result = Open(_sut.Generate(template, SampleData()));
        var sheet = result.Worksheet(1);

        Assert.Equal("SELIM", sheet.Cell("A1").GetString());
        Assert.Equal("Lot: LOT-01", sheet.Cell("A2").GetString());
        Assert.Equal("NV cân: Nguyễn Văn A", sheet.Cell("A3").GetString());
        Assert.Equal("09/10/2026", sheet.Cell("A4").GetString());
        Assert.Equal("50 EA/BOX (12.5 Kg)", sheet.Cell("A5").GetString());
    }

    // US-28 — Ô chứa {{QrCode}}: xoá token khỏi ô và chèn đúng 1 ảnh QR neo tại ô đó.
    [Fact]
    public void Generate_CoTokenQrCode_XoaTokenVaChenAnhTaiDungO()
    {
        var template = BuildTemplate(s =>
        {
            s.Cell("A1").Value = "{{Model}}";
            s.Cell("C3").Value = "{{QrCode}}";
        });

        using var result = Open(_sut.Generate(template, SampleData()));
        var sheet = result.Worksheet(1);

        Assert.Equal(string.Empty, sheet.Cell("C3").GetString());
        var picture = Assert.Single(sheet.Pictures);
        Assert.Equal("C3", picture.TopLeftCell.Address.ToString());
        Assert.Equal(picture.Width, picture.Height); // QR luôn vuông.
    }

    // US-28 — Token QR nằm trong vùng ô gộp: ảnh co theo cả vùng gộp (lớn hơn 1 ô đơn), neo tại ô đầu vùng.
    [Fact]
    public void Generate_TokenQrCodeTrongVungGop_AnhCoTheoCaVungGop()
    {
        var singleCellTemplate = BuildTemplate(s => s.Cell("B2").Value = "{{QrCode}}");
        var mergedTemplate = BuildTemplate(s =>
        {
            s.Cell("B2").Value = "{{QrCode}}";
            s.Range("B2:D6").Merge();
        });

        using var single = Open(_sut.Generate(singleCellTemplate, SampleData()));
        using var merged = Open(_sut.Generate(mergedTemplate, SampleData()));

        var singlePicture = Assert.Single(single.Worksheet(1).Pictures);
        var mergedPicture = Assert.Single(merged.Worksheet(1).Pictures);
        Assert.Equal("B2", mergedPicture.TopLeftCell.Address.ToString());
        Assert.True(mergedPicture.Width > singlePicture.Width);
    }

    // US-28 AC3 — Template không có {{QrCode}} thì tem in ra như cũ, không chèn ảnh nào.
    [Fact]
    public void Generate_KhongCoTokenQrCode_KhongChenAnh()
    {
        var template = BuildTemplate(s => s.Cell("A1").Value = "{{Model}}");

        using var result = Open(_sut.Generate(template, SampleData()));

        Assert.Empty(result.Worksheet(1).Pictures);
        Assert.Equal("ABC-123", result.Worksheet(1).Cell("A1").GetString());
    }

    // US-28 — Nội dung QR giữ đúng định dạng tem cũ (PrintLabel): Model\Lot\BoxNo\NhânViên\SốLượng\yyyyMMdd.
    [Fact]
    public void BuildQrContent_DungDinhDangTemCu()
    {
        Assert.Equal(@"ABC-123\LOT-01\7\Nguyễn Văn A\50\20261009", PackingLabelGenerator.BuildQrContent(SampleData()));
    }

    [Fact]
    public void Generate_FileKhongPhaiXlsx_NemBusinessRuleException()
    {
        Assert.Throws<BusinessRuleException>(() => _sut.Generate(new byte[] { 1, 2, 3 }, SampleData()));
    }
}
