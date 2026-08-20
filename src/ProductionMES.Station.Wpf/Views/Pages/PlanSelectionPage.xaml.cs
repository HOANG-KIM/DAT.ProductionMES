using System.Windows;
using System.Windows.Controls;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>Trang "Chọn kế hoạch" (US-05b).</summary>
public partial class PlanSelectionPage : Page
{
    private readonly PlanSelectionViewModel _viewModel;
    private readonly ISupervisorSessionService _sessionService;

    public PlanSelectionPage(PlanSelectionViewModel viewModel, ISupervisorSessionService sessionService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _sessionService = sessionService;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadLineInfoCommand.ExecuteAsync(null);
        // LoadStagesAsync tự chọn SelectedStage mặc định (AC1), việc này trigger OnSelectedStageChanged gọi
        // LoadAsync — KHÔNG gọi LoadCommand riêng ở đây nữa để tránh tải danh sách kế hoạch 2 lần thừa.
        await _viewModel.LoadStagesCommand.ExecuteAsync(null);
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

    private void LineStageSequenceButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToLineStageSequence();
    }
}
