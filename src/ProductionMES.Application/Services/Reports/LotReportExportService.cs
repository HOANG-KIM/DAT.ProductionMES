using ClosedXML.Excel;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.Scans;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// Implementation <see cref="ILotReportExportService"/> (US-23/FR-23, phạm vi thu hẹp 19/08/2026 — chỉ FR-21/tab
/// "Theo Lot", chưa làm FR-20/tab "Theo Line"). Tái sử dụng NGUYÊN VẸN <see cref="ILotReportService.GetLotSummaryAsync"/>
/// (Sheet 1) và <see cref="IScanService.GetAllHistoryForLotAsync"/> (Sheet 2, KHÔNG phân trang) — Service này chỉ
/// làm nhiệm vụ trình bày lại 2 nguồn dữ liệu đã có sẵn thành file .xlsx bằng <c>ClosedXML</c>, KHÔNG tự tính toán
/// thêm số liệu nào.
/// </summary>
public class LotReportExportService : ILotReportExportService
{
    private readonly ILotReportService _lotReportService;
    private readonly IScanService _scanService;

    public LotReportExportService(ILotReportService lotReportService, IScanService scanService)
    {
        _lotReportService = lotReportService;
        _scanService = scanService;
    }

    // Cùng bảng màu quy ước OK/NG đã dùng xuyên suốt hệ thống (Andon board — ADR-007, LotReportTab/LineReportTab).
    private static readonly XLColor OkColor = XLColor.FromHtml("#4CAF50");
    private static readonly XLColor NgColor = XLColor.FromHtml("#E53935");
    private static readonly XLColor UnknownColor = XLColor.FromHtml("#9E9E9E");
    // Xanh primary AntD mặc định (web-admin không override theme) — dùng làm màu nhấn tiêu đề/header cho đồng nhất cảm giác với UI.
    private static readonly XLColor AccentColor = XLColor.FromHtml("#1677FF");
    private static readonly XLColor AccentLightColor = XLColor.FromHtml("#E6F4FF");
    private static readonly XLColor InfoLabelFillColor = XLColor.FromHtml("#F5F5F5");
    private static readonly XLColor BorderColor = XLColor.FromHtml("#D9D9D9");

    public async Task<byte[]?> ExportAsync(string lot, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var summary = await _lotReportService.GetLotSummaryAsync(lot, fromUtc, toUtc, cancellationToken);
        if (summary is null)
        {
            // AC2 gốc: "Không tìm thấy Lot" -> Controller quy đổi 404, giống LotReportsController.GetSummary.
            return null;
        }

        var scans = await _scanService.GetAllHistoryForLotAsync(lot, fromUtc, toUtc, cancellationToken);

        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = "Calibri";
        workbook.Style.Font.FontSize = 11;
        BuildSummarySheet(workbook, summary);
        BuildScanDetailSheet(workbook, scans);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Sheet 1 "Tổng hợp" — thông tin tổng quan Lot + bảng theo từng (Line, Công đoạn), đúng field <see cref="LotSummaryDto"/>/<see cref="LotStageRowDto"/> đang hiển thị trên <c>LotReportTab</c>.</summary>
    private static void BuildSummarySheet(XLWorkbook workbook, LotSummaryDto summary)
    {
        var sheet = workbook.Worksheets.Add("Tổng hợp");
        sheet.ShowGridLines = false;

        var titleCell = sheet.Cell(1, 1);
        titleCell.Value = $"BÁO CÁO SẢN XUẤT THEO LOT — {summary.Lot}";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;
        titleCell.Style.Font.FontColor = AccentColor;
        sheet.Range(1, 1, 1, 5).Merge();
        sheet.Row(1).Height = 26;

        var generatedAtCell = sheet.Cell(2, 1);
        generatedAtCell.Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm}";
        generatedAtCell.Style.Font.Italic = true;
        generatedAtCell.Style.Font.FontColor = XLColor.Gray;
        generatedAtCell.Style.Font.FontSize = 9;
        sheet.Range(2, 1, 2, 5).Merge();

        var row = 4;
        AddInfoRow(sheet, ref row, "Mã Lot", summary.Lot);
        AddInfoRow(sheet, ref row, "Model", JoinOrPlaceholder(summary.Models));
        AddInfoRow(sheet, ref row, "Khách hàng", JoinOrPlaceholder(summary.Customers));
        AddInfoRow(sheet, ref row, "Revision", JoinOrPlaceholder(summary.Revisions));
        AddInfoRow(sheet, ref row, "Thời gian bắt đầu",
            // FR-21b: FirstScannedAtUtc đã là giờ local nhà máy (API-Conventions.md mục 10, ngoại lệ), không quy đổi thêm.
            summary.FirstScannedAtUtc.HasValue ? summary.FirstScannedAtUtc.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa xác định");
        AddInfoRow(sheet, ref row, "Số lượng Lot",
            summary.LotTotalQuantity.HasValue ? summary.LotTotalQuantity.Value.ToString() : "Chưa xác định");
        row++;

        var headerRow = row;
        var headers = new[] { "Line", "Công đoạn", "OK", "NG", "Đủ số lượng Lot?" };
        WriteTableHeader(sheet, headerRow, headers);
        row++;

        var firstDataRow = row;
        foreach (var stageRow in summary.Rows)
        {
            sheet.Cell(row, 1).Value = stageRow.LineName;
            sheet.Cell(row, 2).Value = stageRow.StageName;
            sheet.Cell(row, 3).Value = stageRow.OkCount;
            sheet.Cell(row, 3).Style.Font.FontColor = OkColor;
            sheet.Cell(row, 3).Style.Font.Bold = true;
            sheet.Cell(row, 4).Value = stageRow.NgCount;
            sheet.Cell(row, 4).Style.Font.FontColor = stageRow.NgCount > 0 ? NgColor : XLColor.Black;
            sheet.Cell(row, 4).Style.Font.Bold = stageRow.NgCount > 0;

            var (label, color) = stageRow.IsSufficientQuantity switch
            {
                null => ("Chưa xác định", UnknownColor),
                true => ("Đủ", OkColor),
                false => ("Chưa đủ", NgColor),
            };
            var sufficiencyCell = sheet.Cell(row, 5);
            sufficiencyCell.Value = label;
            sufficiencyCell.Style.Font.FontColor = color;
            sufficiencyCell.Style.Font.Bold = true;
            row++;
        }

        var lastDataRow = row - 1;
        if (lastDataRow >= firstDataRow)
        {
            var tableRange = sheet.Range(headerRow, 1, lastDataRow, headers.Length);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableRange.Style.Border.OutsideBorderColor = BorderColor;
            StripeRows(sheet, firstDataRow, lastDataRow, headers.Length);
            sheet.Range(firstDataRow, 3, lastDataRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.Range(firstDataRow, 5, lastDataRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.SheetView.Freeze(headerRow, 0);
            tableRange.SetAutoFilter();
        }

        sheet.Column(1).Width = 16;
        sheet.Column(2).Width = 22;
        sheet.Column(3).Width = 10;
        sheet.Column(4).Width = 10;
        sheet.Column(5).Width = 18;
        sheet.Range(4, 1, 9, 1).Style.Fill.BackgroundColor = InfoLabelFillColor;
    }

    private static void AddInfoRow(IXLWorksheet sheet, ref int row, string label, string value)
    {
        var labelCell = sheet.Cell(row, 1);
        labelCell.Value = label;
        labelCell.Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = value;
        sheet.Range(row, 2, row, 5).Merge();
        row++;
    }

    private static string JoinOrPlaceholder(IReadOnlyList<string> values) =>
        values.Count > 0 ? string.Join(", ", values) : "Chưa xác định";

    /// <summary>Sheet 2 "Chi tiết lượt scan" — TOÀN BỘ lượt scan của Lot (mọi Line/Công đoạn/Result) trong khoảng thời gian đang lọc, tái dùng field <see cref="ScanHistoryItemDto"/> đã có (US-10/US-21).</summary>
    private static void BuildScanDetailSheet(XLWorkbook workbook, IReadOnlyList<ScanHistoryItemDto> scans)
    {
        var sheet = workbook.Worksheets.Add("Chi tiết lượt scan");
        sheet.ShowGridLines = false;

        // Đúng danh sách cột đã chốt với Ban quản lý (KHÔNG thêm cột Line/Công đoạn — WorkStationName đã đủ định
        // danh vì 1 WorkStation gắn cố định 1 Line + 1 Stage, xem CLAUDE.md mục Kiến trúc).
        var headers = new[]
        {
            "Mã tem", "Trạm thực hiện", "Nhân viên", "Thời điểm scan", "Kết quả",
            "Lý do lỗi", "Người xác nhận", "Trạng thái rework", "Người mở khóa rework", "Thời điểm mở khóa", "Ghi chú mở khóa",
        };
        WriteTableHeader(sheet, 1, headers);

        var row = 2;
        foreach (var scan in scans)
        {
            sheet.Cell(row, 1).Value = scan.TagCode;
            sheet.Cell(row, 2).Value = scan.WorkStationName;
            sheet.Cell(row, 3).Value = scan.OperatorNames;
            // ScannedAtUtc đã là giờ local nhà máy (API-Conventions.md mục 10, ngoại lệ), không quy đổi thêm.
            sheet.Cell(row, 4).Value = scan.ScannedAtUtc.ToString("dd/MM/yyyy HH:mm:ss");

            var resultCell = sheet.Cell(row, 5);
            resultCell.Value = ResultLabel(scan.Result);
            resultCell.Style.Font.Bold = true;
            resultCell.Style.Font.FontColor = scan.Result switch
            {
                ScanResult.Ok => OkColor,
                ScanResult.Ng => NgColor,
                _ => UnknownColor,
            };

            sheet.Cell(row, 6).Value = scan.RejectionReason ?? string.Empty;
            sheet.Cell(row, 7).Value = scan.ConfirmedByUserName ?? string.Empty;
            sheet.Cell(row, 8).Value = ReworkStatusLabel(scan.ReworkStatus, scan.ReworkStillNgOccurrence);
            sheet.Cell(row, 9).Value = scan.ReworkUnlockedByUserName ?? string.Empty;
            sheet.Cell(row, 10).Value = scan.ReworkUnlockedAtUtc.HasValue
                ? scan.ReworkUnlockedAtUtc.Value.ToString("dd/MM/yyyy HH:mm:ss")
                : string.Empty;
            sheet.Cell(row, 11).Value = scan.ReworkUnlockNote ?? string.Empty;
            row++;
        }

        var lastRow = row - 1;
        if (lastRow >= 2)
        {
            var tableRange = sheet.Range(1, 1, lastRow, headers.Length);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableRange.Style.Border.OutsideBorderColor = BorderColor;
            StripeRows(sheet, 2, lastRow, headers.Length);
            sheet.SheetView.Freeze(1, 1);
            tableRange.SetAutoFilter();
        }

        var widths = new[] { 16, 20, 20, 18, 12, 24, 16, 24, 18, 18, 24 };
        for (var col = 0; col < widths.Length; col++)
        {
            sheet.Column(col + 1).Width = widths[col];
        }
        sheet.Range(1, 6, Math.Max(lastRow, 1), 6).Style.Alignment.WrapText = true;
        sheet.Range(1, 11, Math.Max(lastRow, 1), 11).Style.Alignment.WrapText = true;
    }

    /// <summary>Vẽ 1 hàng tiêu đề bảng dùng chung cho cả 2 sheet — nền xanh <see cref="AccentColor"/>, chữ trắng đậm, căn giữa, border mỏng.</summary>
    private static void WriteTableHeader(IXLWorksheet sheet, int headerRow, IReadOnlyList<string> headers)
    {
        for (var col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(headerRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = AccentColor;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        sheet.Row(headerRow).Height = 20;
    }

    /// <summary>Kẻ băng màu xen kẽ (zebra stripe) cho vùng dữ liệu — dòng lẻ trắng, dòng chẵn xanh nhạt <see cref="AccentLightColor"/> — để dễ đọc bảng dài.</summary>
    private static void StripeRows(IXLWorksheet sheet, int firstRow, int lastRow, int columnCount)
    {
        for (var r = firstRow; r <= lastRow; r++)
        {
            var rowRange = sheet.Range(r, 1, r, columnCount);
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            rowRange.Style.Border.InsideBorderColor = BorderColor;
            if ((r - firstRow) % 2 == 1)
            {
                rowRange.Style.Fill.BackgroundColor = AccentLightColor;
            }
        }
    }

    /// <summary>Nhãn hiển thị <see cref="ScanResult"/> — cùng bộ nhãn đã dùng ở `web-admin/ScanHistoryDrilldownDrawer.tsx` (RESULT_LABEL), giữ đồng nhất UI/Excel.</summary>
    private static string ResultLabel(ScanResult result) => result switch
    {
        ScanResult.Ok => "OK",
        ScanResult.Ng => "NG",
        ScanResult.DuplicateTag => "Trùng tem",
        ScanResult.PreviousStageNotPassed => "Chưa qua công đoạn trước",
        ScanResult.WaitingReworkUnlock => "Chờ mở khóa rework",
        _ => result.ToString(),
    };

    /// <summary>Nhãn hiển thị <see cref="ReworkStatus"/> — cùng bộ nhãn đã dùng ở `web-admin/ScanHistoryDrilldownDrawer.tsx` (REWORK_STATUS_LABEL), rỗng khi null (không áp dụng — vd Result != Ng).</summary>
    private static string ReworkStatusLabel(ReworkStatus? status, int? stillNgOccurrence)
    {
        if (status is null)
        {
            return string.Empty;
        }

        var label = status switch
        {
            ReworkStatus.NotUnlocked => "Chưa mở khóa",
            ReworkStatus.WaitingRescan => "Đã mở khóa, chờ scan lại",
            ReworkStatus.Fixed => "Đã sửa xong (scan lại OK)",
            ReworkStatus.StillNg => "Đã scan lại nhưng vẫn NG",
            _ => status.ToString()!,
        };

        return status == ReworkStatus.StillNg && stillNgOccurrence.HasValue
            ? $"{label} (lần {stillNgOccurrence.Value})"
            : label;
    }
}
