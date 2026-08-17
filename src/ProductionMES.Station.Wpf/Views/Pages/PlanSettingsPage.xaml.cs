using System.Windows;
using System.Windows.Controls;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>Trang "Cài đặt kế hoạch sản xuất" (US-05).</summary>
public partial class PlanSettingsPage : Page
{
    private readonly PlanSettingsViewModel _viewModel;

    public PlanSettingsPage(PlanSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadLinesCommand.ExecuteAsync(null);
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToHome();
    }

    private void PlanSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToPlanSelection();
    }

    private void LineStageSequenceButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToLineStageSequence();
    }
}
