using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Services.AndonBoard;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.LineStageSequences;
using ProductionMES.Station.Wpf.Services.Lines;
using ProductionMES.Station.Wpf.Services.Navigation;
using ProductionMES.Station.Wpf.Services.ProductionPlans;
using ProductionMES.Station.Wpf.Services.ProductionPlanStages;
using ProductionMES.Station.Wpf.Services.Realtime;
using ProductionMES.Station.Wpf.Services.ReworkUnlocks;
using ProductionMES.Station.Wpf.Services.Scans;
using ProductionMES.Station.Wpf.Services.Stages;
using ProductionMES.Station.Wpf.ViewModels;
using ProductionMES.Station.Wpf.Views;
using ProductionMES.Station.Wpf.Views.Pages;

namespace ProductionMES.Station.Wpf;

/// <summary>
/// Khởi tạo DI container (Generic Host) và 2 cửa sổ theo ADR-006 (AndonBoardWindow luôn hiển thị mặc định,
/// MainWindow chỉ hiện khi Tổ trưởng chuyển sang). Không dùng <c>StartupUri</c> trong App.xaml vì cần tự kiểm
/// soát thứ tự khởi tạo DI trước khi tạo Window.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Lưới an toàn: nếu 1 exception nào đó lọt qua hết mọi try/catch trong ViewModel (vd lỗi ngoài dự kiến,
        // không phải ApiException/HttpRequestException/TaskCanceledException đã xử lý riêng), WPF mặc định sẽ
        // im lặng đóng ứng dụng — Tổ trưởng thấy y hệt triệu chứng "bấm không có phản hồi gì". Hiện rõ lỗi thay
        // vì để crash âm thầm.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Đã xảy ra lỗi ngoài dự kiến:\n\n{args.Exception.Message}",
                "Lỗi",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            })
            .ConfigureServices((context, services) => ConfigureServices(context.Configuration, services))
            .Build();

        _host.Start();

        var coordinator = _host.Services.GetRequiredService<IWindowCoordinator>();
        var andonBoardWindow = _host.Services.GetRequiredService<AndonBoardWindow>();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        coordinator.Register(andonBoardWindow, mainWindow);

        andonBoardWindow.Show();

        // US-07 AC2: mở kết nối SignalR ngay khi app khởi động, không chờ scan đầu tiên — lỗi kết nối (mạng/
        // server chưa chạy) không throw ra ngoài (xem ScanHubClient.StartAsync), luồng scan qua HTTP vẫn hoạt
        // động độc lập nếu hub tạm thời không tới được.
        _ = _host.Services.GetRequiredService<IScanHubClient>().StartAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Services.GetService<IScanHubClient>()?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        var stationOptions = new StationOptions();
        configuration.Bind(stationOptions);
        services.AddSingleton(stationOptions);

        services.AddSingleton<ISupervisorSessionService, SupervisorSessionService>();
        services.AddSingleton<IWindowCoordinator, WindowCoordinator>();
        services.AddSingleton<IScanHubClient, ScanHubClient>();
        services.AddTransient<SupervisorAuthHandler>();
        services.AddTransient<StationApiKeyHandler>();

        services.AddHttpClient<ISupervisorAuthService, SupervisorAuthService>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        });

        services.AddHttpClient<IProductionPlanApiClient, ProductionPlanApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<SupervisorAuthHandler>();

        services.AddHttpClient<IProductionPlanStageApiClient, ProductionPlanStageApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<SupervisorAuthHandler>();

        services.AddHttpClient<IStageApiClient, StageApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<SupervisorAuthHandler>();

        services.AddHttpClient<ILineStageSequenceApiClient, LineStageSequenceApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<SupervisorAuthHandler>();

        services.AddHttpClient<ILineApiClient, LineApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<SupervisorAuthHandler>();

        // US-19: "Mở khóa rework" — Bearer Tổ trưởng (Scan.ReworkUnlock), cùng nhóm với các client MainWindow ở trên.
        services.AddHttpClient<IReworkUnlockApiClient, ReworkUnlockApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<SupervisorAuthHandler>();

        // US-07: xác thực bằng API Key theo trạm (ADR-005, scheme StationApiKey) — KHÔNG dùng SupervisorAuthHandler
        // (Bearer, luồng Tổ trưởng nâng quyền).
        services.AddHttpClient<IScanApiClient, ScanApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<StationApiKeyHandler>();

        // US-09: cùng scheme StationApiKey với IScanApiClient (không cần đăng nhập Supervisor).
        services.AddHttpClient<IAndonBoardApiClient, AndonBoardApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<StationOptions>().ApiBaseUrl);
        }).AddHttpMessageHandler<StationApiKeyHandler>();

        services.AddSingleton<AndonBoardWindow>();
        services.AddSingleton<AndonBoardViewModel>();
        services.AddSingleton<MainWindow>();

        services.AddTransient<HomePage>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<PlanSettingsPage>();
        services.AddTransient<PlanSettingsViewModel>();
        services.AddTransient<PlanSelectionPage>();
        services.AddTransient<PlanSelectionViewModel>();
        services.AddTransient<LineStageSequencePage>();
        services.AddTransient<LineStageSequenceViewModel>();
        services.AddTransient<ReworkUnlockPage>();
        services.AddTransient<ReworkUnlockViewModel>();
        services.AddTransient<LoginDialog>();
        services.AddTransient<LoginDialogViewModel>();
    }
}
