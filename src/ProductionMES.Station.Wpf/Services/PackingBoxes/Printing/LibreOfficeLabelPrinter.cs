using System.Diagnostics;
using System.IO;
using ProductionMES.Station.Wpf.Configuration;

namespace ProductionMES.Station.Wpf.Services.PackingBoxes.Printing;

/// <summary>
/// In qua LibreOffice ở chế độ headless (không mở cửa sổ): <c>soffice --headless -p file</c> (máy in mặc định) hoặc
/// <c>soffice --headless --pt "máy in" file</c>. Dùng profile LibreOffice riêng (<c>-env:UserInstallation</c>) để
/// lệnh in không bị "dính" vào cửa sổ LibreOffice người dùng đang mở sẵn trên máy (khi đó soffice chuyển lệnh sang
/// instance cũ và thoát ngay, không in).
/// </summary>
public class LibreOfficeLabelPrinter : ILabelPrinter
{
    private readonly LabelPrintOptions _options;

    public LibreOfficeLabelPrinter(StationOptions stationOptions)
    {
        _options = stationOptions.LabelPrint;
    }

    public string DisplayName => "LibreOffice";

    public bool IsAvailable() => FindSofficePath() != null;

    public async Task PrintAsync(string filePath, string printerName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var sofficePath = FindSofficePath()
            ?? throw new InvalidOperationException("Không tìm thấy LibreOffice (soffice.exe) — cài LibreOffice hoặc khai báo LabelPrint:LibreOfficePath trong appsettings.json của trạm.");

        var profileDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProductionMES", "libreoffice-profile");
        Directory.CreateDirectory(profileDirectory);

        var startInfo = new ProcessStartInfo(sofficePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add($"-env:UserInstallation={new Uri(profileDirectory).AbsoluteUri}");
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--norestore");
        startInfo.ArgumentList.Add("--nologo");
        if (string.IsNullOrWhiteSpace(printerName))
        {
            startInfo.ArgumentList.Add("-p");
        }
        else
        {
            startInfo.ArgumentList.Add("--pt");
            startInfo.ArgumentList.Add(printerName.Trim());
        }
        startInfo.ArgumentList.Add(filePath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Không khởi động được LibreOffice.");

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw;
        }

        if (process.ExitCode != 0)
        {
            var stderr = (await stderrTask).Trim();
            throw new InvalidOperationException(
                $"LibreOffice trả về mã lỗi {process.ExitCode}{(stderr.Length > 0 ? $": {stderr}" : string.Empty)}");
        }
    }

    private string? FindSofficePath()
    {
        if (!string.IsNullOrWhiteSpace(_options.LibreOfficePath))
        {
            return File.Exists(_options.LibreOfficePath) ? _options.LibreOfficePath : null;
        }

        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        };

        return candidates
            .Where(root => !string.IsNullOrEmpty(root))
            .Select(root => Path.Combine(root, "LibreOffice", "program", "soffice.exe"))
            .FirstOrDefault(File.Exists);
    }
}
