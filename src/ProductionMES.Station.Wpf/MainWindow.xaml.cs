using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Views.Pages;

namespace ProductionMES.Station.Wpf;

/// <summary>
/// Cửa sổ chứa các trang chế độ Tổ trưởng (ADR-006): Trang chủ ⇄ Cài đặt kế hoạch (US-05) ⇄ Chọn kế hoạch
/// (US-05b) ⇄ Trình tự công đoạn của Line (US-03) ⇄ Mở khóa rework (US-19), điều hướng nội bộ qua
/// <see cref="Frame"/>, không mở thêm Window riêng cho từng trang.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(IServiceProvider serviceProvider, StationOptions options)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        Title = $"DAT ProductionMES — Trạm {options.WorkStationName}";

        NavigateToHome();
    }

    public void NavigateToHome() => MainFrame.Navigate(_serviceProvider.GetRequiredService<HomePage>());

    public void NavigateToPlanSettings() => MainFrame.Navigate(_serviceProvider.GetRequiredService<PlanSettingsPage>());

    public void NavigateToPlanSelection() => MainFrame.Navigate(_serviceProvider.GetRequiredService<PlanSelectionPage>());

    public void NavigateToLineStageSequence() => MainFrame.Navigate(_serviceProvider.GetRequiredService<LineStageSequencePage>());

    public void NavigateToReworkUnlock() => MainFrame.Navigate(_serviceProvider.GetRequiredService<ReworkUnlockPage>());
}
