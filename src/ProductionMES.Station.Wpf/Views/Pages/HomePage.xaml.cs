using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.Services.Navigation;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>
/// Trang chủ (Main Screen) — launcher tối giản: thông tin trạm + 4 lối vào chế độ Tổ trưởng, mỗi lối yêu cầu
/// đăng nhập Supervisor nếu chưa có phiên đang mở (ADR-005), rồi điều hướng nội bộ qua <see cref="MainWindow"/>.
/// </summary>
public partial class HomePage : Page
{
    private readonly ISupervisorSessionService _sessionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowCoordinator _coordinator;

    public HomePage(HomeViewModel viewModel, ISupervisorSessionService sessionService, IServiceProvider serviceProvider, IWindowCoordinator coordinator)
    {
        InitializeComponent();
        DataContext = viewModel;
        _sessionService = sessionService;
        _serviceProvider = serviceProvider;
        _coordinator = coordinator;
    }

    private void PlanSettingsTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToPlanSettings();
        }
    }

    private void PlanSelectionTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToPlanSelection();
        }
    }

    private void LineStageSequenceTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToLineStageSequence();
        }
    }

    private void ReworkUnlockTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToReworkUnlock();
        }
    }

    private void BackToAndonButton_Click(object sender, RoutedEventArgs e)
    {
        _coordinator.ShowAndonBoard();
    }

    /// <summary>Trả về true nếu đã (hoặc vừa) đăng nhập Supervisor thành công — false nếu người dùng bấm Hủy ở dialog.</summary>
    private bool RequireAuth()
    {
        if (_sessionService.IsAuthenticated)
        {
            return true;
        }

        var dialog = _serviceProvider.GetRequiredService<Views.LoginDialog>();
        dialog.Owner = Window.GetWindow(this);
        var result = dialog.ShowDialog();
        return result == true;
    }
}
