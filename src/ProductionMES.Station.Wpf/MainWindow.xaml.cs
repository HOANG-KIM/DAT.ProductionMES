using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.Services.Navigation;
using ProductionMES.Station.Wpf.Views.Pages;

namespace ProductionMES.Station.Wpf;

/// <summary>
/// Cửa sổ chứa các trang chế độ Tổ trưởng (ADR-006): Trang chủ ⇄ Cài đặt kế hoạch (US-05) ⇄ Chọn kế hoạch
/// (US-05b) ⇄ Trình tự công đoạn của Line (US-03) ⇄ Mở khóa rework (US-19) ⇄ Cấu hình đóng gói theo Model
/// (US-24), điều hướng nội bộ qua <see cref="Frame"/>, không mở thêm Window riêng cho từng trang.
/// </summary>
public partial class MainWindow : Window
{
    private const int GwlStyle = -16;
    private const int WsSysMenu = 0x80000;

    private readonly IServiceProvider _serviceProvider;
    private readonly ISupervisorSessionService _sessionService;
    private readonly IWindowCoordinator _coordinator;
    private readonly DispatcherTimer _idleTimer;

    /// <summary>true chỉ khi <see cref="AllowExit"/> đã được gọi (qua nút "Thoát ứng dụng" ở Trang chủ, xem
    /// <c>WindowCoordinator.ExitApplication</c>) — mọi lần Closing khác (X đã ẩn nhưng vẫn phòng hờ, Alt+F4 ở
    /// môi trường không chặn được WS_SYSMENU) vẫn bị chặn, chỉ ẩn + quay lại Andon Board.</summary>
    private bool _exitAllowed;

    public MainWindow(IServiceProvider serviceProvider, StationOptions options, ISupervisorSessionService sessionService, IWindowCoordinator coordinator)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _sessionService = sessionService;
        _coordinator = coordinator;
        Title = $"DAT ProductionMES — Trạm {options.WorkStationName}";

        // MainWindow là Singleton (App.xaml.cs) — đóng thật (Close()) sẽ hủy Window vĩnh viễn, WPF không cho
        // Show() lại 1 Window đã đóng, khiến lần sau bấm Esc để vào Main Screen bị crash (InvalidOperationException)
        // và Tổ trưởng kẹt cứng ở Andon Board. "Thoát ứng dụng" chỉ nên đi qua đúng 1 đường: nút "Thoát ứng dụng"
        // ở Trang chủ (HomePage.xaml.cs, gọi WindowCoordinator.ExitApplication). 2 lớp phòng vệ ở đây:
        // 1) Ẩn hẳn nút X (SourceInitialized -> HideCloseButton, gỡ WS_SYSMENU) để không ai vô tình bấm nhầm.
        // 2) Lưới an toàn nếu vẫn có Close() gọi tới từ đâu đó (vd Alt+F4 ở môi trường WS_SYSMENU không chặn được
        //    hết, RDP...) — huỷ việc đóng thật, chỉ ẩn và quay lại Andon Board, giống hệt nút "Quay lại Andon Board".
        SourceInitialized += (_, _) => HideCloseButton();
        Closing += (_, e) =>
        {
            if (_exitAllowed)
            {
                return;
            }

            e.Cancel = true;
            Hide();
            _coordinator.ShowAndonBoard();
        };

        // Timeout không hoạt động (idle) của phiên Tổ trưởng dùng chung (PlanSettings/PlanSelection/
        // LineStageSequence/StationConfig — ADR-006), KHÔNG áp dụng cho ReworkUnlockPage (đã có re-auth riêng,
        // US-19 AC7). Là idle timer thật: reset lại mỗi khi có tương tác (Preview* bubble từ mọi control con
        // trong MainFrame), không đếm cố định kể từ lúc đăng nhập — tránh đá Tổ trưởng ra giữa chừng khi đang
        // nhập dở form nhiều trường ở "Cài đặt kế hoạch sản xuất". Cấu hình cục bộ theo trạm (StationOptions.
        // SupervisorSessionIdleTimeoutSeconds), Math.Max phòng cấu hình sai (0/số âm) làm timer bắn ngay lập tức.
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, options.SupervisorSessionIdleTimeoutSeconds)) };
        _idleTimer.Tick += (_, _) =>
        {
            _idleTimer.Stop();
            _sessionService.Clear();
            NavigateToHome();
        };

        _sessionService.SessionChanged += OnSessionChanged;

        PreviewMouseDown += OnUserActivity;
        PreviewKeyDown += OnUserActivity;
        PreviewMouseWheel += OnUserActivity;

        NavigateToHome();
    }

    /// <summary>Vừa đăng nhập (IsAuthenticated chuyển true) -> bắt đầu đếm; vừa clear (dù do bấm "← Trang chủ" hay do chính idle timeout) -> dừng đếm, tránh tick thừa.</summary>
    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _idleTimer.Stop();
        if (_sessionService.IsAuthenticated)
        {
            _idleTimer.Start();
        }
    }

    private void OnUserActivity(object sender, RoutedEventArgs e)
    {
        if (!_sessionService.IsAuthenticated)
        {
            return;
        }

        _idleTimer.Stop();
        _idleTimer.Start();
    }

    public void NavigateToHome() => MainFrame.Navigate(_serviceProvider.GetRequiredService<HomePage>());

    public void NavigateToPlanSettings() => MainFrame.Navigate(_serviceProvider.GetRequiredService<PlanSettingsPage>());

    public void NavigateToPlanSelection() => MainFrame.Navigate(_serviceProvider.GetRequiredService<PlanSelectionPage>());

    public void NavigateToLineStageSequence() => MainFrame.Navigate(_serviceProvider.GetRequiredService<LineStageSequencePage>());

    public void NavigateToReworkUnlock() => MainFrame.Navigate(_serviceProvider.GetRequiredService<ReworkUnlockPage>());

    public void NavigateToStationConfig() => MainFrame.Navigate(_serviceProvider.GetRequiredService<StationConfigPage>());

    public void NavigateToPackingModelConfig() => MainFrame.Navigate(_serviceProvider.GetRequiredService<PackingModelConfigPage>());

    /// <summary>Gỡ khóa Closing đúng 1 lần cho luồng "Thoát ứng dụng" chủ động — gọi từ <c>WindowCoordinator.ExitApplication</c>.</summary>
    public void AllowExit() => _exitAllowed = true;

    /// <summary>Gỡ WS_SYSMENU khỏi style Win32 của Window — ẩn nút X + icon/menu hệ thống góc trái, không có API quản lý (managed) nào làm được việc này, phải P/Invoke user32.dll. Giữ nguyên nút Minimize/Maximize (không thuộc WS_SYSMENU) và tiêu đề.</summary>
    private void HideCloseButton()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GwlStyle);
        SetWindowLong(hwnd, GwlStyle, style & ~WsSysMenu);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
