using System.Collections.ObjectModel;
using System.Media;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.AndonBoard;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.Realtime;
using ProductionMES.Station.Wpf.Services.Scans;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel cho <c>AndonBoardWindow</c> (US-07 AC2-AC5, US-09 AC1-AC6) — nhận ký tự HID scan (tích luỹ tới
/// Enter) từ code-behind, gọi <see cref="IScanApiClient"/>, hiển thị banner OK/NG theo mockup đã chốt, cập nhật
/// "Số lượng đã scan OK" theo thời gian thực qua <see cref="IScanHubClient"/>, và quản lý bảng PLAN/ACTUAL/
/// BALANCE theo mốc giờ (<see cref="Rows"/>, US-09) — tải qua <see cref="IAndonBoardApiClient"/>.
/// </summary>
public partial class AndonBoardViewModel : ObservableObject
{
    /// <summary>AC3: banner OK tự đóng sau 1-2 giây — chọn 1.5s giữa khoảng đó.</summary>
    private static readonly TimeSpan OkBannerAutoCloseDelay = TimeSpan.FromSeconds(1.5);

    private readonly IScanApiClient _scanApiClient;
    private readonly IScanHubClient _scanHubClient;
    private readonly IAndonBoardApiClient _andonBoardApiClient;
    private readonly StationOptions _options;

    /// <summary>Chống 2 lượt scan chạy chồng lấp (vd Enter gõ liên tiếp quá nhanh) làm banner hiển thị sai trạng thái — luồng scan cơ bản xử lý tuần tự từng tem.</summary>
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    private readonly DispatcherTimer _autoCloseTimer;

    /// <summary>
    /// US-09 AC4/AC6: làm mới toàn bộ bảng theo chu kỳ (<see cref="StationOptions.AndonBoardRefreshIntervalSeconds"/>)
    /// để PLAN "trôi" theo thời gian và phát hiện mốc giờ mới xuất hiện — KHÔNG dùng timer này để cập nhật ACTUAL
    /// theo từng lượt scan (xem <see cref="OnScanRecorded"/>, tái sử dụng SignalR để đạt độ trễ ≤ 1s).
    /// </summary>
    private readonly DispatcherTimer _boardRefreshTimer;

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

    /// <summary>
    /// US-09: false khi (Line, Công đoạn) của trạm chưa có kế hoạch nào đang Running (<c>AndonBoardDto.HasActivePlan</c>)
    /// — mặc định false trước khi tải xong lần đầu (<see cref="InitializeAsync"/>), <c>AndonBoardWindow.xaml</c>
    /// hiển thị thông báo thay cho bảng trong lúc đó.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoActivePlan))]
    private bool hasActivePlan;

    public bool NoActivePlan => !HasActivePlan;

    /// <summary>US-09 AC1/AC5: sản lượng kế hoạch (PLAN) lũy kế đến hiện tại — đồng bộ với dòng "hiện tại" cuối cùng của <see cref="Rows"/>.</summary>
    [ObservableProperty]
    private int planCumulative;

    /// <summary>US-09 AC1/AC2/AC3: chênh lệch (BALANCE) = ScannedOkCount − PlanCumulative.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BalanceLabel))]
    [NotifyPropertyChangedFor(nameof(BalanceBrush))]
    private int balance;

    public string BalanceLabel => Balance > 0 ? $"+{Balance}" : Balance.ToString();

    public Brush BalanceBrush => AndonBoardColors.GetBalanceBrush(Balance);

    /// <summary>US-09: tổng NG lũy kế toàn ca — 1 chỉ số gộp duy nhất cho cả bảng (không tách theo mốc giờ).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NgSummaryLabel))]
    private int ngCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NgSummaryLabel))]
    private decimal ngPercent;

    public string NgSummaryLabel => $"{NgCount}/{NgPercent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%";

    /// <summary>US-09 AC6: các dòng theo mốc giờ — phần tử cuối cùng luôn là dòng "hiện tại" (<c>IsCurrent</c> = true).</summary>
    public ObservableCollection<AndonBoardRowViewModel> Rows { get; } = new();

    public AndonBoardViewModel(
        IScanApiClient scanApiClient, IScanHubClient scanHubClient, IAndonBoardApiClient andonBoardApiClient, StationOptions options)
    {
        _scanApiClient = scanApiClient;
        _scanHubClient = scanHubClient;
        _andonBoardApiClient = andonBoardApiClient;
        _options = options;
        WorkStationName = options.WorkStationName;
        StageName = options.StageName;

        _autoCloseTimer = new DispatcherTimer { Interval = OkBannerAutoCloseDelay };
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer.Stop();
            CloseBanner();
        };

        // US-09 AC4/AC6: chu kỳ làm mới toàn bộ bảng (PLAN "trôi" theo thời gian + phát hiện mốc giờ mới) — tối
        // thiểu 5s để phòng cấu hình sai (0 hoặc số âm) làm timer chạy dồn dập, không cần thiết cho nhu cầu thực tế.
        _boardRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(5, options.AndonBoardRefreshIntervalSeconds)) };
        _boardRefreshTimer.Tick += async (_, _) => await RefreshBoardAsync();
        _boardRefreshTimer.Start();

        _scanHubClient.ScanRecorded += OnScanRecorded;
    }

    /// <summary>Gọi 1 lần khi Window khởi tạo xong (US-09) — tải dữ liệu bảng lần đầu, không chờ tick đầu tiên của <see cref="_boardRefreshTimer"/>.</summary>
    public Task InitializeAsync() => RefreshBoardAsync();

    /// <summary>
    /// US-09 AC4/AC6: tải lại toàn bộ bảng từ server. Lỗi mạng/server tạm thời KHÔNG hiển thị cho công nhân (chỉ
    /// giữ nguyên dữ liệu bảng đang có) — luồng scan (mục tiêu chính của màn hình) không được phép bị gián đoạn
    /// vì màn hình phụ trợ này lỗi.
    /// </summary>
    private async Task RefreshBoardAsync()
    {
        AndonBoardDto board;
        try
        {
            board = await _andonBoardApiClient.GetAsync();
        }
        catch
        {
            return;
        }

        Application.Current?.Dispatcher.Invoke(() => ApplyBoard(board));
    }

    private void ApplyBoard(AndonBoardDto board)
    {
        HasActivePlan = board.HasActivePlan;
        PlanCumulative = board.PlanCumulative;
        Balance = board.Balance;
        NgCount = board.NgCount;
        NgPercent = board.NgPercent;
        // AC1: đồng bộ lại "Số lượng đã scan OK" với server (nguồn sự thật) mỗi chu kỳ làm mới — giữa 2 lần làm
        // mới, giá trị này vẫn tăng tức thời qua OnScanRecorded (AC4) như US-07 đã làm.
        ScannedOkCount = board.ActualCumulative;

        Rows.Clear();
        foreach (var row in board.Rows)
        {
            Rows.Add(new AndonBoardRowViewModel
            {
                TimeMarkLocal = row.TimeMarkLocal,
                PlanCumulative = row.PlanCumulative,
                ActualCumulative = row.ActualCumulative,
                Balance = row.Balance,
                IsCurrent = row.IsCurrent,
            });
        }
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

        Application.Current?.Dispatcher.Invoke(() =>
        {
            ScannedOkCount++;

            // US-09 AC4: tăng ACTUAL/BALANCE ngay tại chỗ (độ trễ ≤ 1s) — không chờ chu kỳ làm mới toàn bộ bảng
            // (_boardRefreshTimer, vốn chỉ dành cho PLAN "trôi" theo thời gian). Chỉ có ý nghĩa khi đã có kế
            // hoạch active (Rows rỗng nếu HasActivePlan = false).
            if (!HasActivePlan || Rows.Count == 0)
            {
                return;
            }

            Balance++;

            var currentRow = Rows[^1];
            currentRow.ActualCumulative++;
            currentRow.Balance++;
        });
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
