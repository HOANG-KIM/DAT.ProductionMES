using System.Collections.ObjectModel;
using System.Media;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.AndonBoard;
using ProductionMES.Station.Wpf.Services.Auth;
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

    /// <summary>
    /// US-18 AC2: mã vạch cố định dùng để kích hoạt Chế độ Scan NG (dán tại bàn thao tác, KHÔNG phải tem sản
    /// phẩm thật) — chọn literal "NG" (không có trong SRS/backlog gốc, ghi chú quyết định này ở
    /// `Documents/BACKLOG-user-story.md` mục US-18). So khớp chính xác (case-sensitive, sau khi Trim) để tránh
    /// nhầm với 1 mã tem thật (rủi ro chấp nhận được — tem sản phẩm thực tế không đặt tên "NG").
    /// </summary>
    internal const string NgModeActivationBarcode = "NG";

    /// <summary>
    /// US-18 (thay đổi yêu cầu 18/08/2026): permission bắt buộc để hoàn tất đăng nhập ở AC2a — literal
    /// "Scan.ConfirmNg" (KHÔNG tham chiếu <c>PermissionPolicies</c> của <c>ProductionMES.Api</c> — Station.Wpf
    /// KHÔNG được reference project backend nào, xem CLAUDE.md mục Kiến trúc), phải khớp đúng
    /// <c>DbSeeder.SeedPermissionsAsync</c>/<c>PermissionPolicies.ScanConfirmNg</c> phía server.
    /// </summary>
    internal const string ScanConfirmNgPermission = "Scan.ConfirmNg";

    private readonly IScanApiClient _scanApiClient;
    private readonly IScanHubClient _scanHubClient;
    private readonly IAndonBoardApiClient _andonBoardApiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly StationOptions _options;

    /// <summary>
    /// US-18 (thay đổi 18/08/2026): access token đăng nhập RIÊNG cho lượt Scan NG hiện tại (AC2a), gán bởi
    /// <see cref="AuthenticateForNgMode"/> — dùng đúng 1 lần cho <see cref="ConfirmNgReasonAsync"/>, xóa lại
    /// trong <see cref="DeactivateNgMode"/> (bất kể hoàn tất/hủy/timeout) để không bao giờ tái sử dụng cho lượt
    /// Scan NG kế tiếp (AC2a "KHÔNG yêu cầu đăng nhập thêm lần nào nữa trong SUỐT PHẦN CÒN LẠI CỦA LƯỢT NÀY").
    /// KHÔNG ghi vào <see cref="ISupervisorSessionService"/> dùng chung.
    /// </summary>
    private string? _ngScanAccessToken;

    /// <summary>Chống 2 lượt scan chạy chồng lấp (vd Enter gõ liên tiếp quá nhanh) làm banner hiển thị sai trạng thái — luồng scan cơ bản xử lý tuần tự từng tem.</summary>
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    private readonly DispatcherTimer _autoCloseTimer;

    /// <summary>
    /// US-09 AC4/AC6: làm mới toàn bộ bảng theo chu kỳ (<see cref="StationOptions.AndonBoardRefreshIntervalSeconds"/>)
    /// để PLAN "trôi" theo thời gian và phát hiện mốc giờ mới xuất hiện — KHÔNG dùng timer này để cập nhật ACTUAL
    /// theo từng lượt scan (xem <see cref="OnScanRecorded"/>, tái sử dụng SignalR để đạt độ trễ ≤ 1s).
    /// </summary>
    private readonly DispatcherTimer _boardRefreshTimer;

    /// <summary>Đồng hồ DATE/TIME ở header (mockup 13/08/2026) — tick mỗi giây, không gọi API, độc lập với <see cref="_boardRefreshTimer"/>.</summary>
    private readonly DispatcherTimer _clockTimer;

    /// <summary>
    /// US-18 AC7: đếm ngược kể từ lúc kích hoạt Chế độ Scan NG — hết giờ mà CHƯA quét tem nào (<see cref="PendingNgTagCode"/>
    /// vẫn null) thì tự động quay về Chế độ Scan OK, không lưu gì. Dừng lại ngay khi đã quét được 1 tem (chuyển
    /// sang bước nhập lý do — AC7 chỉ áp dụng cho giai đoạn "chưa quét tem nào").
    /// </summary>
    private readonly DispatcherTimer _ngModeTimeoutTimer;

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

    /// <summary>Header bổ sung theo mockup 13/08/2026 (artifact "Station Andon Board") — Model sản phẩm, rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    [ObservableProperty]
    private string model = string.Empty;

    /// <summary>Lot sản xuất — rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    [ObservableProperty]
    private string lot = string.Empty;

    /// <summary>PROD.PLAN — số lượng kế hoạch, 0 khi <see cref="HasActivePlan"/> = false.</summary>
    [ObservableProperty]
    private int plannedQuantity;

    /// <summary>TAKT TIME đã format phút:giây (tái dùng <see cref="TaktTimeFormat.ToDisplay"/> đã dùng ở US-05), rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    [ObservableProperty]
    private string taktTimeLabel = string.Empty;

    /// <summary>STARTING TIME đã format "HH:mm", rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    [ObservableProperty]
    private string startingTimeLabel = string.Empty;

    /// <summary>Ô PER — danh sách tên nhân viên vận hành, free text, rỗng khi <see cref="HasActivePlan"/> = false.</summary>
    [ObservableProperty]
    private string operatorNamesLabel = string.Empty;

    /// <summary>Đồng hồ DATE (dd/MM) hiển thị góc trên-trái header, cập nhật mỗi giây, độc lập với dữ liệu kế hoạch.</summary>
    [ObservableProperty]
    private string currentDateLabel = string.Empty;

    /// <summary>Đồng hồ TIME (HH:mm:ss) hiển thị góc trên-trái header, cập nhật mỗi giây, độc lập với dữ liệu kế hoạch.</summary>
    [ObservableProperty]
    private string currentTimeLabel = string.Empty;

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

    /// <summary>
    /// US-18 AC1/AC2: true khi trạm đang ở Chế độ Scan NG — <c>AndonBoardWindow.xaml</c> bind vào đây để đổi nền
    /// đỏ + hiển thị thông báo lớn "ĐANG Ở CHẾ ĐỘ NG". Bao trùm cả 2 bước con (chờ quét tem lỗi VÀ đang nhập lý
    /// do — <see cref="IsNgReasonPanelVisible"/>), vì cả 2 bước đều thuộc "Chế độ Scan NG" theo AC1/AC2.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWaitingForNgTagCode))]
    private bool isNgModeActive;

    /// <summary>US-18 AC3: true sau khi đã quét tem lỗi (<see cref="PendingNgTagCode"/> khác null), đang chờ nhập lý do.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWaitingForNgTagCode))]
    private bool isNgReasonPanelVisible;

    /// <summary>Mã tem sản phẩm lỗi đã quét trong Chế độ Scan NG, đang chờ nhập lý do — null nếu chưa quét tem nào.</summary>
    [ObservableProperty]
    private string? pendingNgTagCode;

    /// <summary>US-18 AC1: true khi đang ở Chế độ Scan NG nhưng CHƯA quét tem nào — dùng để hiển thị đúng thông báo chờ quét (khác thông báo/form nhập lý do).</summary>
    public bool IsWaitingForNgTagCode => IsNgModeActive && !IsNgReasonPanelVisible;

    /// <summary>US-18 AC3/AC4: lý do lỗi đang nhập (free text) — TextBox reason panel bind 2 chiều vào đây.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmNgReasonCommand))]
    private string ngReasonText = string.Empty;

    /// <summary>US-18 AC4: danh sách gợi ý autocomplete cho công đoạn hiện tại, tải lại mỗi lần bắt đầu nhập lý do.</summary>
    public ObservableCollection<string> NgReasonSuggestions { get; } = new();

    public AndonBoardViewModel(
        IScanApiClient scanApiClient, IScanHubClient scanHubClient, IAndonBoardApiClient andonBoardApiClient,
        IServiceProvider serviceProvider, StationOptions options)
    {
        _scanApiClient = scanApiClient;
        _scanHubClient = scanHubClient;
        _andonBoardApiClient = andonBoardApiClient;
        _serviceProvider = serviceProvider;
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

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        UpdateClock();
        _clockTimer.Start();

        // US-18 AC7: timeout mặc định 30s, cấu hình cục bộ theo trạm (StationOptions.NgModeTimeoutSeconds) —
        // Math.Max(1, ...) phòng cấu hình sai (0/số âm) làm timer bắn ngay lập tức.
        _ngModeTimeoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, options.NgModeTimeoutSeconds)) };
        _ngModeTimeoutTimer.Tick += (_, _) =>
        {
            _ngModeTimeoutTimer.Stop();
            // AC7: hết timeout mà CHƯA quét tem nào -> tự quay về Scan OK, không ảnh hưởng lượt scan tiếp theo
            // (không lưu bất kỳ bản ghi Scan nào cho lần kích hoạt NG mode này).
            DeactivateNgMode();
        };

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

    /// <summary>Cập nhật đồng hồ DATE/TIME ở header — gọi mỗi tick <see cref="_clockTimer"/> và 1 lần lúc khởi tạo.</summary>
    private void UpdateClock()
    {
        var now = DateTime.Now;
        CurrentDateLabel = now.ToString("dd/MM");
        CurrentTimeLabel = now.ToString("HH:mm:ss");
    }

    private void ApplyBoard(AndonBoardDto board)
    {
        HasActivePlan = board.HasActivePlan;
        Model = board.Model;
        Lot = board.Lot;
        PlannedQuantity = board.PlannedQuantity;
        TaktTimeLabel = board.HasActivePlan ? TaktTimeFormat.ToVerboseDisplay(board.TaktTimeSeconds) : string.Empty;
        StartingTimeLabel = board.HasActivePlan && board.PlanStartTime.HasValue ? board.PlanStartTime.Value.ToString("HH:mm") : string.Empty;
        OperatorNamesLabel = board.OperatorNames;
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

    /// <summary>
    /// Xử lý 1 lượt scan hoàn chỉnh (đủ ký tự HID + Enter) — gọi từ code-behind Window khi bắt được sự kiện gõ
    /// tem, dùng chung cho cả máy quét thật, ô nhập tay (US-07), lẫn mã vạch "NG" cố định (US-18 AC2).
    /// </summary>
    public async Task HandleScanAsync(string tagCode)
    {
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            return;
        }

        tagCode = tagCode.Trim();

        // US-18 AC2: mã vạch "NG" cố định kích hoạt/gia hạn Chế độ Scan NG, KHÔNG đi qua API scan bình thường.
        if (tagCode.Equals(NgModeActivationBarcode, StringComparison.Ordinal))
        {
            await ActivateNgModeAsync();
            return;
        }

        if (IsNgModeActive)
        {
            if (PendingNgTagCode is not null)
            {
                // AC3: đang chờ nhập lý do cho tem đã quét trước đó — bỏ qua tem quét thêm cho tới khi hoàn tất/hủy.
                return;
            }

            await _scanLock.WaitAsync();
            try
            {
                await BeginNgReasonEntryAsync(tagCode);
            }
            finally
            {
                _scanLock.Release();
            }
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

    /// <summary>
    /// US-18 AC1/AC2/AC2a/AC2b/AC2c (thay đổi yêu cầu 18/08/2026): kích hoạt Chế độ Scan NG (gọi từ nút bấm hoặc
    /// mã vạch "NG") — BẮT BUỘC đăng nhập lại Tổ trưởng (re-auth mỗi lần, <see cref="AuthenticateForNgMode"/>)
    /// NGAY LẬP TỨC TRƯỚC KHI đổi giao diện/vào Chế độ Scan NG (AC1/AC2 "CHƯA đổi giao diện nền đỏ ... cho tới
    /// khi đăng nhập thành công"). Nếu đã active và chưa quét tem nào (đã đăng nhập từ trước cho đúng lượt này),
    /// chỉ gia hạn lại timeout (AC7), KHÔNG đăng nhập lại lần 2 (AC2a "KHÔNG yêu cầu đăng nhập thêm lần nào nữa").
    /// </summary>
    [RelayCommand]
    private Task ActivateNgModeAsync()
    {
        if (IsNgModeActive && PendingNgTagCode is null)
        {
            RestartNgTimeoutTimer();
            return Task.CompletedTask;
        }

        if (!AuthenticateForNgMode())
        {
            // AC2b (sai tài khoản/thiếu quyền, dialog tự ở lại cho tới khi Hủy)/AC2c (bấm Hủy) -> KHÔNG vào Chế
            // độ Scan NG, quay lại Scan OK bình thường như chưa bấm nút "NG"/quét mã "NG".
            return Task.CompletedTask;
        }

        _autoCloseTimer.Stop();
        CloseBanner();

        IsNgModeActive = true;
        IsNgReasonPanelVisible = false;
        PendingNgTagCode = null;
        NgReasonText = string.Empty;
        NgReasonSuggestions.Clear();
        // AC7: mốc bắt đầu đếm 30s là NGAY SAU KHI đăng nhập thành công (AuthenticateForNgMode đã trả về true ở
        // trên) — thời gian hiển thị popup đăng nhập KHÔNG tính vào 30 giây này.
        RestartNgTimeoutTimer();
        return Task.CompletedTask;
    }

    /// <summary>
    /// US-18 AC1/AC2/AC2a/AC2b/AC2c: hiển thị popup đăng nhập Tổ trưởng (re-auth mỗi lần, tái sử dụng
    /// <see cref="Views.LoginDialog"/>/<see cref="LoginDialogViewModel"/> đã có ở US-05/05a/05b, nhưng KHÔNG tái
    /// dùng phần "session còn hạn thì bỏ qua" của <c>HomePage.RequireAuth</c>). Trả về true (VÀ gán
    /// <see cref="_ngScanAccessToken"/>) nếu đăng nhập thành công + có quyền <see cref="ScanConfirmNgPermission"/>;
    /// false nếu bấm Hủy (AC2c) — trường hợp sai tài khoản/thiếu quyền (AC2b) dialog tự ở lại (xem
    /// <see cref="LoginDialogViewModel.LoginAsync"/>), chỉ trả về false khi người dùng chủ động đóng dialog bằng Hủy.
    /// </summary>
    private bool AuthenticateForNgMode()
    {
        var dialog = _serviceProvider.GetRequiredService<Views.LoginDialog>();
        dialog.ViewModel.RequiredPermission = ScanConfirmNgPermission;
        dialog.Owner = Application.Current?.MainWindow;

        var loggedIn = dialog.ShowDialog() == true;
        if (!loggedIn)
        {
            return false;
        }

        _ngScanAccessToken = dialog.ViewModel.NgConfirmationLoginResult?.AccessToken;
        return !string.IsNullOrEmpty(_ngScanAccessToken);
    }

    /// <summary>AC3 bước 1: đã quét tem lỗi — mở form nhập lý do + tải gợi ý autocomplete (AC4) theo đúng công đoạn trạm.</summary>
    private async Task BeginNgReasonEntryAsync(string tagCode)
    {
        // AC7 chỉ tính "chưa quét tem nào" — đã quét được 1 tem nên dừng đếm ngược, không giới hạn thời gian nhập lý do.
        _ngModeTimeoutTimer.Stop();

        PendingNgTagCode = tagCode;
        NgReasonText = string.Empty;
        NgReasonSuggestions.Clear();
        IsNgReasonPanelVisible = true;

        try
        {
            var suggestions = await _scanApiClient.GetNgReasonSuggestionsAsync(_options.StageId);
            foreach (var suggestion in suggestions)
            {
                NgReasonSuggestions.Add(suggestion);
            }
        }
        catch
        {
            // Lỗi tải gợi ý KHÔNG chặn nhập tay tự do (AC4 chỉ là gợi ý, không bắt buộc chọn từ danh sách).
        }
    }

    /// <summary>AC3/AC5/AC6: xác nhận lý do -> gọi API ghi Scan NG -> tự động quay về Chế độ Scan OK.</summary>
    [RelayCommand(CanExecute = nameof(CanConfirmNgReason))]
    private async Task ConfirmNgReasonAsync()
    {
        var tagCode = PendingNgTagCode;
        var reason = NgReasonText.Trim();
        if (tagCode is null || reason.Length == 0)
        {
            return;
        }

        // US-18 AC2a: giữ lại token đã đăng nhập ở AuthenticateForNgMode TRƯỚC KHI DeactivateNgMode xóa nó —
        // dùng đúng 1 lần cho request xác nhận NG này, không đăng nhập lại lần 2.
        var accessToken = _ngScanAccessToken;

        await _scanLock.WaitAsync();
        try
        {
            // Thoát khỏi nền đỏ Chế độ Scan NG NGAY khi bắt đầu gửi lên server (không đợi có kết quả) — nếu vẫn
            // giữ overlay đỏ trong lúc chờ, nó sẽ che mất banner WAITING/kết quả (2 UI cùng vẽ chồng lên nhau).
            // AC6 vẫn thỏa: mode đã quay về Scan OK "ngay khi lưu xong" theo đúng nghĩa với người vận hành, vì họ
            // không thể quét tem nào khác cho tới khi banner kết quả được đóng dù mode đã tắt hay chưa.
            DeactivateNgMode();

            if (string.IsNullOrEmpty(accessToken))
            {
                // Phòng vệ: không nên xảy ra (AC1/AC2/AC2a đã bắt đăng nhập trước khi vào IsNgModeActive) — nhưng
                // nếu có (vd lỗi logic tương lai), từ chối gửi thay vì gọi API chắc chắn 401.
                ShowErrorBanner(tagCode, "Thiếu thông tin đăng nhập Tổ trưởng để xác nhận NG — vui lòng bấm lại nút NG.");
                return;
            }

            ShowWaitingBanner(tagCode);

            ScanResultDto result;
            try
            {
                result = await _scanApiClient.CreateNgAsync(tagCode, _options.WorkStationId, reason, accessToken);
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

            ShowNgRecordedBanner(result.TagCode, reason);
        }
        finally
        {
            _scanLock.Release();
        }
    }

    private bool CanConfirmNgReason() => !string.IsNullOrWhiteSpace(NgReasonText);

    /// <summary>Người vận hành hủy form nhập lý do (vd quét nhầm tem) — quay về Chế độ Scan OK, không lưu gì.</summary>
    [RelayCommand]
    private void CancelNgReason() => DeactivateNgMode();

    private void RestartNgTimeoutTimer()
    {
        _ngModeTimeoutTimer.Stop();
        _ngModeTimeoutTimer.Start();
    }

    /// <summary>AC6/AC7: tắt hẳn Chế độ Scan NG, dọn state — quay về Chế độ Scan OK mặc định.</summary>
    private void DeactivateNgMode()
    {
        _ngModeTimeoutTimer.Stop();
        IsNgModeActive = false;
        IsNgReasonPanelVisible = false;
        PendingNgTagCode = null;
        NgReasonText = string.Empty;
        NgReasonSuggestions.Clear();
        // US-18 AC2a: mỗi lượt Scan NG dùng đúng 1 lần đăng nhập — xóa token dùng riêng cho lượt này bất kể
        // hoàn tất/hủy/timeout, để lượt Scan NG kế tiếp bắt buộc đăng nhập lại (AuthenticateForNgMode).
        _ngScanAccessToken = null;
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

    /// <summary>US-18 AC5/AC6: xác nhận đã ghi nhận Scan NG thành công — dùng màu đỏ giống <see cref="ShowErrorBanner"/> (đây cũng là kết quả NG), khác tiêu đề để phân biệt với lượt scan bị hệ thống tự động từ chối.</summary>
    private void ShowNgRecordedBanner(string tagCode, string reason)
    {
        BannerKind = ScanBannerKind.Error;
        BannerTitle = "NG ĐÃ GHI NHẬN";
        BannerTagCode = tagCode;
        BannerMessage = $"Lý do: {reason}";
        RequiresAcknowledgement = true;
        ApplyBannerColors(ScanBannerKind.Error);
        IsBannerVisible = true;
        SystemSounds.Hand.Play();

        // AC4 (banner): KHÔNG tự đóng — chờ AcknowledgeBannerCommand, giống ShowErrorBanner.
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
