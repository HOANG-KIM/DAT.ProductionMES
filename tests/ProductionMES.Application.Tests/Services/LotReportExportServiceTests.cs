using ClosedXML.Excel;
using Moq;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.Reports;
using ProductionMES.Application.Services.Scans;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="LotReportExportService"/> (US-23/FR-23, phạm vi thu hẹp 19/08/2026 — chỉ báo cáo
/// "Theo Lot"). Mock trực tiếp <see cref="ILotReportService"/>/<see cref="IScanService"/> (KHÔNG mock repository)
/// vì Service này chỉ trình bày lại 2 nguồn dữ liệu đã có sẵn thành file .xlsx, không tự truy vấn DB.
/// </summary>
public class LotReportExportServiceTests
{
    private readonly Mock<ILotReportService> _lotReportServiceMock = new();
    private readonly Mock<IScanService> _scanServiceMock = new();
    private readonly LotReportExportService _sut;

    public LotReportExportServiceTests()
    {
        _sut = new LotReportExportService(_lotReportServiceMock.Object, _scanServiceMock.Object);
    }

    private static LotSummaryDto MakeSummary(string lot = "LOT-A") => new()
    {
        Lot = lot,
        Models = new[] { "M1" },
        Customers = new[] { "C1" },
        Revisions = new[] { "R1" },
        FirstScannedAtUtc = new DateTime(2026, 8, 19, 7, 0, 0),
        LotTotalQuantity = 1000,
        Rows = new List<LotStageRowDto>
        {
            new()
            {
                LineId = 1, LineName = "Line 1", StageId = 10, StageName = "Lắp ráp",
                OkCount = 800, NgCount = 5, IsSufficientQuantity = false,
            },
            new()
            {
                LineId = 1, LineName = "Line 1", StageId = 20, StageName = "Đóng gói",
                OkCount = 1000, NgCount = 0, IsSufficientQuantity = true,
            },
        },
    };

    // AC2 gốc: Lot không tồn tại -> GetLotSummaryAsync trả null -> ExportAsync cũng trả null, KHÔNG throw, KHÔNG gọi tiếp GetAllHistoryForLotAsync.
    [Fact]
    public async Task ExportAsync_LotKhongTonTai_TraVeNull_KhongGoiLayLichSuScan()
    {
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-KHONG-TON-TAI", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LotSummaryDto?)null);

        var result = await _sut.ExportAsync("LOT-KHONG-TON-TAI", null, null);

        Assert.Null(result);
        _scanServiceMock.Verify(
            s => s.GetAllHistoryForLotAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // AC1 gốc "chỉ hỗ trợ .xlsx": file trả về phải mở được bằng ClosedXML và có đúng 2 sheet theo tên đã chốt.
    [Fact]
    public async Task ExportAsync_LotTonTai_SinhFileCoDung2Sheet()
    {
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSummary());
        _scanServiceMock
            .Setup(s => s.GetAllHistoryForLotAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ScanHistoryItemDto>());

        var result = await _sut.ExportAsync("LOT-A", null, null);

        Assert.NotNull(result);
        using var workbook = new XLWorkbook(new MemoryStream(result!));
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.True(workbook.Worksheets.Contains("Tổng hợp"));
        Assert.True(workbook.Worksheets.Contains("Chi tiết lượt scan"));
    }

    // Sheet 1: thông tin tổng quan Lot + bảng breakdown theo (Line, Công đoạn) đúng dữ liệu LotSummaryDto (không bịa thêm).
    [Fact]
    public async Task ExportAsync_Sheet1TongHop_ChuaDungThongTinTongQuanVaBangBreakdown()
    {
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSummary());
        _scanServiceMock
            .Setup(s => s.GetAllHistoryForLotAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ScanHistoryItemDto>());

        var result = await _sut.ExportAsync("LOT-A", null, null);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Tổng hợp");

        var allText = sheet.RangeUsed()!.CellsUsed().Select(c => c.GetString()).ToList();
        Assert.Contains("LOT-A", allText);
        Assert.Contains("M1", allText);
        Assert.Contains("C1", allText);
        Assert.Contains("R1", allText);
        Assert.Contains("1000", allText); // Số lượng Lot.
        Assert.Contains("Line 1", allText);
        Assert.Contains("Lắp ráp", allText);
        Assert.Contains("Đóng gói", allText);
        Assert.Contains("800", allText); // OkCount dòng Lắp ráp.
        Assert.Contains("Chưa đủ", allText);
        Assert.Contains("Đủ", allText);
    }

    // US-21a AC6: LotTotalQuantity = null -> hiển thị "Chưa xác định", KHÔNG để trống/0.
    [Fact]
    public async Task ExportAsync_LotTotalQuantityChuaXacDinh_HienThiChuaXacDinh()
    {
        var summary = MakeSummary();
        summary.LotTotalQuantity = null;
        summary.FirstScannedAtUtc = null;
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _scanServiceMock
            .Setup(s => s.GetAllHistoryForLotAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ScanHistoryItemDto>());

        var result = await _sut.ExportAsync("LOT-A", null, null);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Tổng hợp");
        var allText = sheet.RangeUsed()!.CellsUsed().Select(c => c.GetString()).ToList();

        Assert.Equal(2, allText.Count(t => t == "Chưa xác định")); // 1 cho Thời gian bắt đầu, 1 cho Số lượng Lot.
    }

    // Sheet 2: lấy TẤT CẢ bản ghi trả về từ GetAllHistoryForLotAsync (không giới hạn 200 dòng như UI), đúng field đã chốt.
    [Fact]
    public async Task ExportAsync_Sheet2ChiTietLuotScan_ChuaDayDuTatCaBanGhiDungField()
    {
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSummary());

        var scans = Enumerable.Range(1, 250) // > 200 để khẳng định KHÔNG bị giới hạn như phân trang UI.
            .Select(i => new ScanHistoryItemDto
            {
                Id = i,
                TagCode = $"TAG{i}",
                WorkStationName = "Trạm 1",
                OperatorNames = "Nguyễn Văn A",
                ScannedAtUtc = new DateTime(2026, 8, 19, 8, 0, 0).AddMinutes(i),
                Result = ScanResult.Ok,
            })
            .ToList();
        // Thêm 1 bản ghi NG kèm đầy đủ thông tin rework để kiểm tra field NG-only.
        scans.Add(new ScanHistoryItemDto
        {
            Id = 999,
            TagCode = "TAG-NG",
            WorkStationName = "Trạm 1",
            OperatorNames = "Nguyễn Văn A",
            ScannedAtUtc = new DateTime(2026, 8, 19, 9, 0, 0),
            Result = ScanResult.Ng,
            RejectionReason = "Trầy xước",
            ConfirmedByUserName = "to-truong-1",
            ReworkStatus = ReworkStatus.Fixed,
            ReworkUnlockedByUserName = "to-truong-2",
            ReworkUnlockedAtUtc = new DateTime(2026, 8, 19, 9, 30, 0),
            ReworkUnlockNote = "Đã sửa xong",
        });

        _scanServiceMock
            .Setup(s => s.GetAllHistoryForLotAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scans);

        var result = await _sut.ExportAsync("LOT-A", null, null);

        using var workbook = new XLWorkbook(new MemoryStream(result!));
        var sheet = workbook.Worksheet("Chi tiết lượt scan");

        // Header row (1) + 251 dòng dữ liệu.
        Assert.Equal(252, sheet.RangeUsed()!.RowCount());

        var allText = sheet.RangeUsed()!.CellsUsed().Select(c => c.GetString()).ToList();
        Assert.Contains("TAG-NG", allText);
        Assert.Contains("Trầy xước", allText);
        Assert.Contains("to-truong-1", allText);
        Assert.Contains("to-truong-2", allText);
        Assert.Contains("Đã sửa xong", allText);
        Assert.Contains(allText, t => t.Contains("Đã sửa xong"));
        Assert.Contains(allText, t => t == "NG");
        Assert.Contains(allText, t => t == "OK");
    }
}
