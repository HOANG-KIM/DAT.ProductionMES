using System.Media;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.Realtime;
using ProductionMES.Station.Wpf.Services.Scans;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel cho <c>AndonBoardWindow</c> (US-07 AC2-AC5) — nhận ký tự HID scan (tích luỹ tới Enter) từ code-behind,
/// gọi <see cref="IScanApiClient"/>, hiển thị banner OK/NG theo mockup đã chốt, và cập nhật "Số lượng đã scan OK"
/// theo thời gian thực qua <see cref="IScanHubClient"/> (không chỉ dựa vào response của chính request vừa gửi).
/// </summary>
public partial class AndonBoardViewModel : ObservableObject
{
    /// <summary>AC3: banner OK tự đóng sau 1-2 giây — chọn 1.5s giữa khoảng đó.</summary>
    private static readonly TimeSpan OkBannerAutoCloseDelay = TimeSpan.FromSeconds(1.5);

    private readonly IScanApiClient _scanApiClient;
    private readonly IScanHubClient _scanHubClient;
    private readonly StationOptions _options;

    /// <summary>Chống 2 lượt scan chạy chồng lấp (vd Enter gõ liên tiếp quá nhanh) làm banner hiển thị sai trạng thái — luồng scan cơ bản xử lý tuần tự từng tem.</summary>
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    private readonly DispatcherTimer _autoCloseTimer;

    public string WorkStationName { get; }

    public string StageName { get; }

    /// <summary>
    /// US-07, cờ cấu hình cục bộ (quyết định 17/08/2026 — SRS mục 5.1/8.2): true khi trạm này đang bật "chế độ
    /// nhập tay/test" (<see cref="StationOptions.EnableManualScanInput"/>). <c>AndonBoardWindow.xaml</c> bind vào
    /// đây để hiển thị/ẩn khu vực nhập tay + banner cảnh báo liên tục — không ảnh hưởng luồng <c>ScanInputBox</c>
    /// hiện có (máy quét thật luôn hoạt động dù cờ tắt).
    /// </summary>
    public bool IsManualScanInputEnabled => _options.EnableManualScanInput;

    [ObservableProperty]
    private string manualScanInputText = string.Empty;

    [ObservableProperty]
    private int scannedOkCount;

    [ObservableProperty]
    private bool isBannerVisible;

    [ObservableProperty]
    private ScanBannerKind bannerKind = ScanBannerKind.None;

    [ObservableProperty]
    private string bannerTitle = string.Empty;

    [ObservableProperty]
    private string bannerTagCode = string.Empty;

    [ObservableProperty]
    private string bannerMessage = string.Empty;

    /// <summary>AC4: true khi banner lỗi — hiện nút "Đã đọc, đóng", ẩn khi banner OK (tự đóng, không cần nút).</summary>
    [ObservableProperty]
    private bool requiresAcknowledgement;

    [ObservableProperty]
    private Brush bannerBackground = Brushes.Transparent;

    [ObservableProperty]
    private Brush bannerBorderBrush = Brushes.Transparent;

    [ObservableProperty]
    private Brush bannerForeground = Brushes.White;

    public AndonBoardViewModel(IScanApiClient scanApiClient, IScanHubClient scanHubClient, StationOptions options)
    {
        _scanApiClient = scanApiClient;
        _scanHubClient = scanHubClient;
        _options = options;
        WorkStationName = options.WorkStationName;
        StageName = options.StageName;

        _autoCloseTimer = new DispatcherTimer { Interval = OkBannerAutoCloseDelay };
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer.Stop();
            CloseBanner();
        };

        _scanHubClient.ScanRecorded += OnScanRecorded;
    }

    /// <summary>Xử lý 1 lượt scan hoàn chỉnh (đủ ký tự HID + Enter) — gọi từ code-behind Window khi bắt được sự kiện gõ tem.</summary>
    public async Task HandleScanAsync(string tagCode)
    {
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            return;
        }

        await _scanLock.WaitAsync();
        try
        {
            _autoCloseTimer.Stop();
            ShowWaitingBanner(tagCode);

            ScanResultDto result;
            try
            {
                result = await _scanApiClient.CreateAsync(tagCode, _options.WorkStationId);
            }
            catch (ApiException ex)
            {
                ShowErrorBanner(tagCode, ex.Message);
                return;
            }
            catch (HttpRequestException ex)
            {
                ShowErrorBanner(tagCode, NetworkErrorMessage.ForConnectionFailure(ex));
                return;
            }
            catch (TaskCanceledException)
            {
                ShowErrorBanner(tagCode, NetworkErrorMessage.ForTimeout());
                return;
            }

            if (result.Result == ScanResult.Ok)
            {
                // AC2: KHÔNG tự tăng ScannedOkCount ở đây — nguồn cập nhật số lượng là sự kiện SignalR
                // ScanRecorded (OnScanRecorded), để nhất quán nếu sau này có nhiều nguồn ghi scan khác gọi cùng
                // trạm (theo đúng ghi chú kỹ thuật đã chốt cho US-07).
                ShowOkBanner(result.TagCode);
            }
            else
            {
                ShowErrorBanner(result.TagCode, result.RejectionReason ?? "Scan bị từ chối.");
            }
        }
        finally
        {
            _scanLock.Release();
        }
    }

    /// <summary>AC4: người vận hành bấm xác nhận đã đọc để đóng banner lỗi.</summary>
    [RelayCommand]
    private void AcknowledgeBanner() => CloseBanner();

    /// <summary>
    /// Khu vực nhập tay (chỉ hiển thị khi <see cref="IsManualScanInputEnabled"/>) — gọi lại đúng
    /// <see cref="HandleScanAsync"/>, dùng chung luồng banner OK/NG/Waiting với input máy quét thật, không xử lý
    /// kết quả riêng.
    /// </summary>
    [RelayCommand]
    private async Task SubmitManualScanAsync()
    {
        var tagCode = ManualScanInputText.Trim();
        ManualScanInputText = string.Empty;

        if (tagCode.Length > 0)
        {
            await HandleScanAsync(tagCode);
        }
    }

    private void OnScanRecorded(ScanResultDto dto)
    {
        if (dto.Result != ScanResult.Ok || dto.WorkStationId != _options.WorkStationId)
        {
            // Server đã lọc theo group đúng trạm (ScanHub.GetStationGroupName) nên điều kiện WorkStationId ở
            // đây chỉ là lớp bảo vệ thêm phía client, không tin tưởng tuyệt đối.
            return;
        }

        Application.Current?.Dispatcher.Invoke(() => ScannedOkCount++);
    }

    private void ShowWaitingBanner(string tagCode)
    {
        BannerKind = ScanBannerKind.Waiting;
        BannerTitle = "WAITING...";
        BannerTagCode = tagCode;
        BannerMessage = "Đang gửi kết quả scan lên server...";
        RequiresAcknowledgement = false;
        ApplyBannerColors(ScanBannerKind.Waiting);
        IsBannerVisible = true;
    }

    private void ShowOkBanner(string tagCode)
    {
        BannerKind = ScanBannerKind.Ok;
        BannerTitle = "OK INPUT";
        BannerTagCode = tagCode;
        BannerMessage = "Scan hợp lệ.";
        RequiresAcknowledgement = false;
        ApplyBannerColors(ScanBannerKind.Ok);
        IsBannerVisible = true;
        SystemSounds.Beep.Play();

        // AC3: tự đóng sau 1-2 giây, không chặn scan tiếp theo (_scanLock đã release trước khi tới đây).
        _autoCloseTimer.Stop();
        _autoCloseTimer.Start();
    }

    private void ShowErrorBanner(string tagCode, string message)
    {
        BannerKind = ScanBannerKind.Error;
        BannerTitle = "NG INPUT";
        BannerTagCode = tagCode;
        BannerMessage = message;
        RequiresAcknowledgement = true;
        ApplyBannerColors(ScanBannerKind.Error);
        IsBannerVisible = true;
        SystemSounds.Hand.Play();

        // AC4: KHÔNG tự đóng — chờ AcknowledgeBannerCommand.
        _autoCloseTimer.Stop();
    }

    private void CloseBanner()
    {
        IsBannerVisible = false;
        RequiresAcknowledgement = false;
        BannerKind = ScanBannerKind.None;
    }

    private void ApplyBannerColors(ScanBannerKind kind)
    {
        (BannerBackground, BannerBorderBrush) = kind switch
        {
            ScanBannerKind.Ok => (FreezeBrush("#1F5D34"), FreezeBrush("#4CAF50")),
            ScanBannerKind.Error => (FreezeBrush("#6B1B1B"), FreezeBrush("#E53935")),
            ScanBannerKind.Waiting => (FreezeBrush("#6B4E00"), FreezeBrush("#FFC107")),
            _ => (Brushes.Transparent, Brushes.Transparent),
        };
        BannerForeground = Brushes.White;
    }

    private static SolidColorBrush FreezeBrush(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
