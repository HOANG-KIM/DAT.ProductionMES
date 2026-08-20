using System.Windows;
using System.Windows.Controls;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>Trang "Trình tự công đoạn của Line" (US-03) — cấu hình 1 lần cho Line, dùng chung mọi kế hoạch chạy trên Line đó.</summary>
public partial class LineStageSequencePage : Page
{
    private readonly LineStageSequenceViewModel _viewModel;
    private readonly ISupervisorSessionService _sessionService;

    public LineStageSequencePage(LineStageSequenceViewModel viewModel, ISupervisorSessionService sessionService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _sessionService = sessionService;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadLineInfoCommand.ExecuteAsync(null);
        await _viewModel.LoadStagesCommand.ExecuteAsync(null);
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Nút "← Trang chủ" là tín hiệu "đã xong việc, chủ động rời khu vực nâng quyền" — clear session ngay trước
    /// khi điều hướng, không chờ idle timeout.
    /// </summary>
    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        _sessionService.Clear();
        ((MainWindow)Window.GetWindow(this)!).NavigateToHome();
    }

    private void PlanSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToPlanSettings();
    }

    private void PlanSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToPlanSelection();
    }
}
