using System.Windows;
using System.Windows.Controls;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>Trang "Chọn kế hoạch" (US-05b).</summary>
public partial class PlanSelectionPage : Page
{
    private readonly PlanSelectionViewModel _viewModel;

    public PlanSelectionPage(PlanSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadLineInfoCommand.ExecuteAsync(null);
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
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
