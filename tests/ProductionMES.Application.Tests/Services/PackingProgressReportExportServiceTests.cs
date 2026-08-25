using ClosedXML.Excel;
using Moq;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.Reports;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="PackingProgressReportExportService"/> (US-26/FR-26, AC9-AC13 — bổ sung 25/08/2026).
/// Mock trực tiếp <see cref="IPackingProgressReportService"/> (KHÔNG mock repository) vì Service này chỉ trình bày
/// lại 3 nguồn dữ liệu đã có sẵn (<c>GetReportAsync</c>/<c>GetBoxesAsync</c>/<c>GetBoxScansAsync</c>) thành file
/// .xlsx, không tự truy vấn DB.
/// </summary>
public class PackingProgressReportExportServiceTests
{
    private const int LineId = 1;
    private const string Lot = "LOT-A";

    private readonly Mock<IPackingProgressReportService> _reportServiceMock = new();
    private readonly PackingProgressReportExportService _sut;

    public PackingProgressReportExportServiceTests()
    {
        _sut = new PackingProgressReportExportService(_reportServiceMock.Object);
    }

    private static PackingProgressReportRowDto MakeRow(
        int? lotTotalQuantity = 100, decimal? completionPercentage = 40m, bool? isSufficientQuantity = false) => new()
    {
        ProductionPlanId = 10,
        LineId = LineId,
        LineName = "Line 1",
        StageId = 100,
        StageName = "Đóng thùng",
        Model = "M1",
        Lot = Lot,
        CompletedBoxCount = 2,
        PackedOkQuantity = 40,
        LotTotalQuantity = lotTotalQuantity,
        CompletionPercentage = completionPercentage,
        IsSufficientQuantity = isSufficientQuantity,
    };

    private void SetupReport(PackingProgressReportRowDto? row)
    {
        _reportServiceMock
            .Setup(s => s.GetReportAsync(
                It.Is<PackingProgressReportQuery>(q => q.LineId == LineId && q.Lot == Lot),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PackingProgressReportDto
            {
                GeneratedAtUtc = DateTime.UtcNow,
                Rows = row is null ? Array.Empty<PackingProgressReportRowDto>() : new[] { row },
            });
    }

    private void SetupBoxes(IReadOnlyList<PackingProgressReportBoxDto> boxes) =>
        _reportServiceMock
            .Setup(s => s.GetBoxesAsync(LineId, Lot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(boxes);

    private void SetupBoxScans(int boxId, PackingProgressReportBoxScansDto scans) =>
        _reportServiceMock
            .Setup(s => s.GetBoxScansAsync(boxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scans);

    // AC9: KHÔNG còn dòng nào khớp Line + Lot tại thời điểm xuất -> trả về null (Controller quy đổi 404), KHÔNG gọi tiếp GetBoxesAsync.
    [Fact]
    public async Task ExportAsync_KhongCoDongKhopLineVaLot_TraVeNull_KhongGoiTiepGetBoxesAsync()
    {
        SetupReport(null);

        var result = await _sut.ExportAsync(LineId, Lot);

        Assert.Null(result);
        _reportServiceMock.Verify(s => s.GetBoxesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC9: file sinh ra phải mở được bằng ClosedXML và có đúng 3 sheet theo tên đã chốt.
    [Fact]
    public async Task ExportAsync_CoDongKhop_SinhFileCoDung3Sheet()
    {
        SetupReport(MakeRow());
        SetupBoxes(Array.Empty<PackingProgressReportBoxDto>());

        var result = await _sut.ExportAsync(LineId, Lot);

        Assert.NotNull(result);
        using var workbook = new XLWorkbook(new MemoryStream(result!));
        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.True(workbook.Worksheets.Contains("Tổng quan"));
        Assert.True(workbook.Worksheets.Contains("Danh sách thùng"));
        Assert.True(workbook.Worksheets.Contains("Lượt scan"));
    }

    // AC10: sheet "Tổng quan" hiển thị đúng Line/Model/Lot/số thùng/tổng SL OK/Tổng số lượng Lot/% hoàn thành/nhãn Đủ-Chưa đủ của ĐÚNG dòng đã xuất.
    [Fact]
    public async Task ExportAsync_SheetTongQuan_AC10_HienThiDungThongTinCuaDong()
    {
        SetupReport(MakeRow(lotTotalQuantity: 100, completionPercentage: 40m, isSufficientQuantity: false));
        SetupBoxes(Array.Empty<PackingProgressReportBoxDto>());

        var result = await _sut.ExportAsync(LineId, Lot);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Tổng quan");
        var allText = sheet.RangeUsed()!.CellsUsed().Select(c => c.GetString()).ToList();

        Assert.Contains("Line 1", allText);
        Assert.Contains("M1", allText);
        Assert.Contains(Lot, allText);
        Assert.Contains("2", allText); // CompletedBoxCount.
        Assert.Contains("40", allText); // PackedOkQuantity.
        Assert.Contains("100", allText); // LotTotalQuantity.
        Assert.Contains("40%", allText); // CompletionPercentage.
        Assert.Contains("Chưa đủ", allText);
    }

    // AC10/AC3: Lot chưa có "Tổng số lượng Lot" -> hiển thị "Chưa xác định" cho % hoàn thành + nhãn trạng thái, KHÔNG suy diễn 0%.
    [Fact]
    public async Task ExportAsync_SheetTongQuan_AC10_LotChuaCoTongSoLuong_HienThiChuaXacDinh()
    {
        SetupReport(MakeRow(lotTotalQuantity: null, completionPercentage: null, isSufficientQuantity: null));
        SetupBoxes(Array.Empty<PackingProgressReportBoxDto>());

        var result = await _sut.ExportAsync(LineId, Lot);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Tổng quan");
        var allText = sheet.RangeUsed()!.CellsUsed().Select(c => c.GetString()).ToList();

        Assert.Equal(3, allText.Count(t => t == "Chưa xác định")); // Tổng số lượng Lot + % hoàn thành + Trạng thái.
        Assert.DoesNotContain("0%", allText);
    }

    // AC11: TẤT CẢ thùng (Completed lẫn InProgress) của Lot, sắp theo Số thùng tăng dần, không giới hạn số dòng.
    [Fact]
    public async Task ExportAsync_SheetDanhSachThung_AC11_TatCaThungSapXepTheoBoxNoTangDan()
    {
        SetupReport(MakeRow());
        SetupBoxes(new[]
        {
            new PackingProgressReportBoxDto { Id = 2, BoxNo = 2, Status = PackingBoxStatus.Completed, ScannedQuantity = 20, TargetQuantity = 20, OpenedAtUtc = new DateTime(2026, 8, 20, 8, 0, 0), CompletedAtUtc = new DateTime(2026, 8, 20, 9, 0, 0) },
            new PackingProgressReportBoxDto { Id = 3, BoxNo = 3, Status = PackingBoxStatus.InProgress, ScannedQuantity = 5, TargetQuantity = 20, OpenedAtUtc = new DateTime(2026, 8, 21, 8, 0, 0), CompletedAtUtc = null },
            new PackingProgressReportBoxDto { Id = 1, BoxNo = 1, Status = PackingBoxStatus.Completed, ScannedQuantity = 20, TargetQuantity = 20, OpenedAtUtc = new DateTime(2026, 8, 19, 8, 0, 0), CompletedAtUtc = new DateTime(2026, 8, 19, 9, 0, 0) },
        });
        SetupBoxScans(1, new PackingProgressReportBoxScansDto { HasDetailedScanData = true, Scans = Array.Empty<PackingProgressReportBoxScanDto>() });
        SetupBoxScans(2, new PackingProgressReportBoxScansDto { HasDetailedScanData = true, Scans = Array.Empty<PackingProgressReportBoxScanDto>() });
        SetupBoxScans(3, new PackingProgressReportBoxScansDto { HasDetailedScanData = true, Scans = Array.Empty<PackingProgressReportBoxScanDto>() });

        var result = await _sut.ExportAsync(LineId, Lot);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Danh sách thùng");

        // Header (1) + 3 dòng dữ liệu.
        Assert.Equal(4, sheet.RangeUsed()!.RowCount());
        var boxNoColumn = sheet.Column(1).CellsUsed().Skip(1).Select(c => c.GetValue<int>()).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, boxNoColumn);

        var allText = sheet.RangeUsed()!.CellsUsed().Select(c => c.GetString()).ToList();
        Assert.Contains("Hoàn tất", allText);
        Assert.Contains("Đang đóng", allText);
    }

    // AC12: gộp lượt scan của TẤT CẢ thùng, thêm cột Số thùng, sắp theo Số thùng rồi theo thời điểm scan tăng dần trong cùng thùng.
    [Fact]
    public async Task ExportAsync_SheetLuotScan_AC12_GomCotSoThungVaSapXepDungThuTu()
    {
        SetupReport(MakeRow());
        SetupBoxes(new[]
        {
            new PackingProgressReportBoxDto { Id = 1, BoxNo = 1, Status = PackingBoxStatus.Completed, ScannedQuantity = 2, TargetQuantity = 2, OpenedAtUtc = new DateTime(2026, 8, 19) },
            new PackingProgressReportBoxDto { Id = 2, BoxNo = 2, Status = PackingBoxStatus.Completed, ScannedQuantity = 1, TargetQuantity = 1, OpenedAtUtc = new DateTime(2026, 8, 20) },
        });
        SetupBoxScans(1, new PackingProgressReportBoxScansDto
        {
            HasDetailedScanData = true,
            Scans = new[]
            {
                new PackingProgressReportBoxScanDto { TagCode = "TAG-1A", ScannedAtUtc = new DateTime(2026, 8, 19, 8, 0, 0) },
                new PackingProgressReportBoxScanDto { TagCode = "TAG-1B", ScannedAtUtc = new DateTime(2026, 8, 19, 8, 5, 0) },
            },
        });
        SetupBoxScans(2, new PackingProgressReportBoxScansDto
        {
            HasDetailedScanData = true,
            Scans = new[] { new PackingProgressReportBoxScanDto { TagCode = "TAG-2A", ScannedAtUtc = new DateTime(2026, 8, 20, 8, 0, 0) } },
        });

        var result = await _sut.ExportAsync(LineId, Lot);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Lượt scan");

        // Header (1) + 3 dòng scan.
        Assert.Equal(4, sheet.RangeUsed()!.RowCount());
        var tagCodeColumn = sheet.Column(2).CellsUsed().Skip(1).Select(c => c.GetString()).ToList();
        Assert.Equal(new[] { "TAG-1A", "TAG-1B", "TAG-2A" }, tagCodeColumn);
        var boxNoColumn = sheet.Column(1).CellsUsed().Skip(1).Select(c => c.GetValue<int>()).ToList();
        Assert.Equal(new[] { 1, 1, 2 }, boxNoColumn);
    }

    // AC8/AC12: thùng KHÔNG có dữ liệu chi tiết lượt scan (thùng cũ) vẫn có 1 dòng ghi rõ Số thùng + ghi chú, không bị bỏ sót.
    [Fact]
    public async Task ExportAsync_SheetLuotScan_AC12_ThungKhongCoDuLieuChiTiet_GhiChuKhongBoSotThung()
    {
        SetupReport(MakeRow());
        SetupBoxes(new[]
        {
            new PackingProgressReportBoxDto { Id = 1, BoxNo = 1, Status = PackingBoxStatus.Completed, ScannedQuantity = 20, TargetQuantity = 20, OpenedAtUtc = new DateTime(2026, 8, 19) },
            new PackingProgressReportBoxDto { Id = 2, BoxNo = 2, Status = PackingBoxStatus.Completed, ScannedQuantity = 5, TargetQuantity = 5, OpenedAtUtc = new DateTime(2026, 8, 20) },
        });
        // Thùng #1: thùng cũ, không có dữ liệu chi tiết (AC8).
        SetupBoxScans(1, new PackingProgressReportBoxScansDto { HasDetailedScanData = false, Scans = Array.Empty<PackingProgressReportBoxScanDto>() });
        // Thùng #2: có dữ liệu chi tiết bình thường.
        SetupBoxScans(2, new PackingProgressReportBoxScansDto
        {
            HasDetailedScanData = true,
            Scans = new[] { new PackingProgressReportBoxScanDto { TagCode = "TAG-2A", ScannedAtUtc = new DateTime(2026, 8, 20, 8, 0, 0) } },
        });

        var result = await _sut.ExportAsync(LineId, Lot);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Lượt scan");

        // Header (1) + 1 dòng ghi chú (thùng 1) + 1 dòng scan (thùng 2) = 3.
        Assert.Equal(3, sheet.RangeUsed()!.RowCount());
        var allText = sheet.RangeUsed()!.CellsUsed().Select(c => c.GetString()).ToList();
        Assert.Contains("Không có dữ liệu chi tiết lượt scan", allText);

        var noteRow = sheet.RowsUsed().First(r => r.Cell(4).GetString() == "Không có dữ liệu chi tiết lượt scan");
        Assert.Equal(1, noteRow.Cell(1).GetValue<int>()); // Số thùng đúng thùng #1.
        Assert.Equal(string.Empty, noteRow.Cell(2).GetString()); // Không có Mã tem.
    }
}
