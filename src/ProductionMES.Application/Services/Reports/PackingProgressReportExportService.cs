using ClosedXML.Excel;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Services.Reports;

/// <summary>
/// Implementation <see cref="IPackingProgressReportExportService"/> (US-26/FR-26, AC9-AC13 — bổ sung 25/08/2026).
/// Chỉ trình bày lại 3 nguồn dữ liệu đã có sẵn ở <see cref="IPackingProgressReportService"/> thành file .xlsx bằng
/// <c>ClosedXML</c> — KHÔNG tự tính toán thêm số liệu nào, cùng tinh thần <see cref="LotReportExportService"/>
/// (US-23, tiền lệ kỹ thuật export Excel của dự án).
/// </summary>
public class PackingProgressReportExportService : IPackingProgressReportExportService
{
    private readonly IPackingProgressReportService _packingProgressReportService;

    public PackingProgressReportExportService(IPackingProgressReportService packingProgressReportService)
    {
        _packingProgressReportService = packingProgressReportService;
    }

    // Cùng bảng màu quy ước OK/NG/nhấn đã dùng xuyên suốt hệ thống (Andon board — ADR-007, LotReportExportService).
    private static readonly XLColor OkColor = XLColor.FromHtml("#4CAF50");
    private static readonly XLColor NgColor = XLColor.FromHtml("#E53935");
    private static readonly XLColor UnknownColor = XLColor.FromHtml("#9E9E9E");
    private static readonly XLColor AccentColor = XLColor.FromHtml("#1677FF");
    private static readonly XLColor AccentLightColor = XLColor.FromHtml("#E6F4FF");
    private static readonly XLColor InfoLabelFillColor = XLColor.FromHtml("#F5F5F5");
    private static readonly XLColor BorderColor = XLColor.FromHtml("#D9D9D9");

    public async Task<byte[]?> ExportAsync(int lineId, string lot, CancellationToken cancellationToken = default)
    {
        // AC9: dữ liệu MỚI NHẤT tại thời điểm xuất — gọi lại cả 3 nguồn ngay tại đây, không dùng dữ liệu cache UI.
        var report = await _packingProgressReportService.GetReportAsync(
            new PackingProgressReportQuery { LineId = lineId, Lot = lot }, cancellationToken);
        var row = report.Rows.FirstOrDefault();
        if (row is null)
        {
            // Không còn dòng nào khớp Line + Lot tại thời điểm xuất (vd kế hoạch không còn Running) -> Controller quy đổi 404.
            return null;
        }

        // AC11: sắp lại theo BoxNo tăng dần ngay tại đây (KHÔNG chỉ dựa vào contract "đã sắp sẵn" của GetBoxesAsync)
        // để đảm bảo đúng thứ tự trong file xuất dù nguồn dữ liệu thay đổi thứ tự trả về trong tương lai.
        var boxes = (await _packingProgressReportService.GetBoxesAsync(lineId, lot, cancellationToken))
            .OrderBy(b => b.BoxNo)
            .ToList();

        // AC12: với MỖI thùng (đã sắp theo BoxNo tăng dần ở trên), lấy chi tiết lượt scan ngay tại đây.
        var boxScans = new List<(PackingProgressReportBoxDto Box, PackingProgressReportBoxScansDto Scans)>();
        foreach (var box in boxes)
        {
            var scans = await _packingProgressReportService.GetBoxScansAsync(box.Id, cancellationToken);
            boxScans.Add((box, scans));
        }

        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = "Calibri";
        workbook.Style.Font.FontSize = 11;
        BuildOverviewSheet(workbook, row);
        BuildBoxListSheet(workbook, boxes);
        BuildBoxScansSheet(workbook, boxScans);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Sheet "Tổng quan" (AC10) — đúng thông tin của dòng báo cáo (Line + Lot) đã bấm xuất, khớp field <see cref="PackingProgressReportRowDto"/> đang hiển thị trên <c>PackingProgressTab</c>.</summary>
    private static void BuildOverviewSheet(XLWorkbook workbook, PackingProgressReportRowDto row)
    {
        var sheet = workbook.Worksheets.Add("Tổng quan");
        sheet.ShowGridLines = false;

        var titleCell = sheet.Cell(1, 1);
        titleCell.Value = $"BÁO CÁO TIẾN ĐỘ ĐÓNG THÙNG — {row.Lot}";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;
        titleCell.Style.Font.FontColor = AccentColor;
        sheet.Range(1, 1, 1, 2).Merge();
        sheet.Row(1).Height = 26;

        var generatedAtCell = sheet.Cell(2, 1);
        generatedAtCell.Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm}";
        generatedAtCell.Style.Font.Italic = true;
        generatedAtCell.Style.Font.FontColor = XLColor.Gray;
        generatedAtCell.Style.Font.FontSize = 9;
        sheet.Range(2, 1, 2, 2).Merge();

        var line = 4;
        AddInfoRow(sheet, ref line, "Line", row.LineName);
        AddInfoRow(sheet, ref line, "Model", row.Model);
        AddInfoRow(sheet, ref line, "Lot", row.Lot);
        AddInfoRow(sheet, ref line, "Số thùng đã đóng", row.CompletedBoxCount.ToString());
        AddInfoRow(sheet, ref line, "Tổng SL đã đóng (OK)", row.PackedOkQuantity.ToString());
        AddInfoRow(sheet, ref line, "Tổng số lượng Lot",
            row.LotTotalQuantity.HasValue ? row.LotTotalQuantity.Value.ToString() : "Chưa xác định");

        var percentRow = line;
        var percentCell = sheet.Cell(percentRow, 1);
        percentCell.Value = "% hoàn thành";
        percentCell.Style.Font.Bold = true;
        var percentValueCell = sheet.Cell(percentRow, 2);
        percentValueCell.Value = row.CompletionPercentage.HasValue ? $"{row.CompletionPercentage.Value}%" : "Chưa xác định";
        percentValueCell.Style.Font.FontColor = row.CompletionPercentage.HasValue ? XLColor.Black : UnknownColor;
        line++;

        var statusRow = line;
        var statusLabelCell = sheet.Cell(statusRow, 1);
        statusLabelCell.Value = "Trạng thái";
        statusLabelCell.Style.Font.Bold = true;
        var (statusLabel, statusColor) = row.IsSufficientQuantity switch
        {
            null => ("Chưa xác định", UnknownColor),
            true => ("Đủ", OkColor),
            false => ("Chưa đủ", NgColor),
        };
        var statusValueCell = sheet.Cell(statusRow, 2);
        statusValueCell.Value = statusLabel;
        statusValueCell.Style.Font.FontColor = statusColor;
        statusValueCell.Style.Font.Bold = true;
        line++;

        sheet.Column(1).Width = 22;
        sheet.Column(2).Width = 32;
        sheet.Range(4, 1, line - 1, 1).Style.Fill.BackgroundColor = InfoLabelFillColor;
    }

    /// <summary>Sheet "Danh sách thùng" (AC11) — TẤT CẢ thùng của Lot (Completed lẫn InProgress), đã sắp theo BoxNo tăng dần từ <c>GetBoxesAsync</c>, KHÔNG giới hạn số dòng.</summary>
    private static void BuildBoxListSheet(XLWorkbook workbook, IReadOnlyList<PackingProgressReportBoxDto> boxes)
    {
        var sheet = workbook.Worksheets.Add("Danh sách thùng");
        sheet.ShowGridLines = false;

        var headers = new[] { "Số thùng", "Số lượng đã quét", "Số lượng mục tiêu", "Trạng thái", "Thời điểm mở thùng", "Thời điểm hoàn tất" };
        WriteTableHeader(sheet, 1, headers);

        var row = 2;
        foreach (var box in boxes)
        {
            sheet.Cell(row, 1).Value = box.BoxNo;
            sheet.Cell(row, 2).Value = box.ScannedQuantity;
            sheet.Cell(row, 3).Value = box.TargetQuantity;

            var statusCell = sheet.Cell(row, 4);
            statusCell.Value = BoxStatusLabel(box.Status);
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Font.FontColor = box.Status == PackingBoxStatus.Completed ? OkColor : XLColor.Black;

            sheet.Cell(row, 5).Value = box.OpenedAtUtc.ToString("dd/MM/yyyy HH:mm:ss");
            sheet.Cell(row, 6).Value = box.CompletedAtUtc.HasValue ? box.CompletedAtUtc.Value.ToString("dd/MM/yyyy HH:mm:ss") : string.Empty;
            row++;
        }

        var lastRow = row - 1;
        if (lastRow >= 2)
        {
            var tableRange = sheet.Range(1, 1, lastRow, headers.Length);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableRange.Style.Border.OutsideBorderColor = BorderColor;
            StripeRows(sheet, 2, lastRow, headers.Length);
            sheet.Range(2, 2, lastRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.SheetView.Freeze(1, 0);
            tableRange.SetAutoFilter();
        }

        var widths = new[] { 12, 18, 18, 16, 20, 20 };
        for (var col = 0; col < widths.Length; col++)
        {
            sheet.Column(col + 1).Width = widths[col];
        }
    }

    /// <summary>
    /// Sheet "Lượt scan" (AC12) — gộp lượt scan OK của TẤT CẢ thùng trong <paramref name="boxScans"/> (đã sắp theo
    /// BoxNo tăng dần), thêm cột Số thùng để đối chiếu chéo với sheet "Danh sách thùng". Thùng KHÔNG có dữ liệu
    /// chi tiết (<see cref="PackingProgressReportBoxScansDto.HasDetailedScanData"/> = <c>false</c>, AC8) vẫn có 1
    /// dòng ghi rõ Số thùng kèm ghi chú thay vì bị bỏ sót.
    /// </summary>
    private static void BuildBoxScansSheet(XLWorkbook workbook, IReadOnlyList<(PackingProgressReportBoxDto Box, PackingProgressReportBoxScansDto Scans)> boxScans)
    {
        var sheet = workbook.Worksheets.Add("Lượt scan");
        sheet.ShowGridLines = false;

        var headers = new[] { "Số thùng", "Mã tem", "Thời điểm scan", "Ghi chú" };
        WriteTableHeader(sheet, 1, headers);

        var row = 2;
        foreach (var (box, scans) in boxScans)
        {
            if (!scans.HasDetailedScanData)
            {
                // AC8/AC12: thùng mở/hoàn tất trước khi triển khai liên kết Scan-PackingBox -> 1 dòng ghi chú, không bỏ sót thùng.
                sheet.Cell(row, 1).Value = box.BoxNo;
                sheet.Cell(row, 2).Value = string.Empty;
                sheet.Cell(row, 3).Value = string.Empty;
                var noteCell = sheet.Cell(row, 4);
                noteCell.Value = "Không có dữ liệu chi tiết lượt scan";
                noteCell.Style.Font.FontColor = UnknownColor;
                noteCell.Style.Font.Italic = true;
                row++;
                continue;
            }

            // scans.Scans đã sắp theo ScannedAtUtc tăng dần từ GetBoxScansAsync -> thứ tự trong cùng thùng đã đúng AC12.
            foreach (var scan in scans.Scans)
            {
                sheet.Cell(row, 1).Value = box.BoxNo;
                sheet.Cell(row, 2).Value = scan.TagCode;
                sheet.Cell(row, 3).Value = scan.ScannedAtUtc.ToString("dd/MM/yyyy HH:mm:ss");
                sheet.Cell(row, 4).Value = string.Empty;
                row++;
            }
        }

        var lastRow = row - 1;
        if (lastRow >= 2)
        {
            var tableRange = sheet.Range(1, 1, lastRow, headers.Length);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableRange.Style.Border.OutsideBorderColor = BorderColor;
            StripeRows(sheet, 2, lastRow, headers.Length);
            sheet.Range(2, 1, lastRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.SheetView.Freeze(1, 0);
            tableRange.SetAutoFilter();
        }

        var widths = new[] { 12, 20, 20, 32 };
        for (var col = 0; col < widths.Length; col++)
        {
            sheet.Column(col + 1).Width = widths[col];
        }
    }

    private static void AddInfoRow(IXLWorksheet sheet, ref int row, string label, string value)
    {
        var labelCell = sheet.Cell(row, 1);
        labelCell.Value = label;
        labelCell.Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = value;
        row++;
    }

    /// <summary>Vẽ 1 hàng tiêu đề bảng — nền xanh <see cref="AccentColor"/>, chữ trắng đậm, căn giữa, border mỏng (cùng style <c>LotReportExportService</c>).</summary>
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

    /// <summary>Kẻ băng màu xen kẽ (zebra stripe) cho vùng dữ liệu — dòng lẻ trắng, dòng chẵn xanh nhạt <see cref="AccentLightColor"/>.</summary>
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

    /// <summary>Nhãn hiển thị <see cref="PackingBoxStatus"/> — cùng bộ nhãn đã dùng ở `web-admin/PackingBoxesModal.tsx`, giữ đồng nhất UI/Excel.</summary>
    private static string BoxStatusLabel(PackingBoxStatus status) => status switch
    {
        PackingBoxStatus.Completed => "Hoàn tất",
        PackingBoxStatus.InProgress => "Đang đóng",
        _ => status.ToString(),
    };
}
