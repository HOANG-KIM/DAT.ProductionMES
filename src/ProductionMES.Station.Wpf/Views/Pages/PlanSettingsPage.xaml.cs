using System.Windows;
using System.Windows.Controls;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>Trang "Cài đặt kế hoạch sản xuất" (US-05).</summary>
public partial class PlanSettingsPage : Page
{
    private readonly PlanSettingsViewModel _viewModel;
    private readonly ISupervisorSessionService _sessionService;

    public PlanSettingsPage(PlanSettingsViewModel viewModel, ISupervisorSessionService sessionService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _sessionService = sessionService;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadLinesCommand.ExecuteAsync(null);
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Nút "← Trang chủ" là tín hiệu "đã xong việc, chủ động rời khu vực nâng quyền" (các trang chế độ Tổ trưởng
    /// đã có nút liên kết chéo trực tiếp qua lại lẫn nhau, không cần vòng qua đây) — clear session ngay trước khi
    /// điều hướng, không chờ idle timeout.
    /// </summary>
    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        _sessionService.Clear();
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

    private void PackingModelConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToPackingModelConfig();
    }

    /// <summary>
    /// US-05 AC1e "Ngoại lệ khi xoá trắng": nếu ô Takt time bị xoá trắng và mất focus mà không nhập lại gì, tự
    /// phục hồi về giá trị mặc định thay vì để trống. Set lại <c>Text</c> sẽ tự đẩy ngược vào
    /// <see cref="ViewModels.PlanSettingsViewModel.TaktTimeDisplay"/> qua binding (đã UpdateSourceTrigger=PropertyChanged).
    /// </summary>
    private void TaktTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            textBox.Text = TaktTimeFormat.ToDisplay(0);
        }
    }

    /// <summary>
    /// US-05 AC1d "Ngoại lệ khi xoá trắng": tương tự <see cref="TaktTimeTextBox_LostFocus"/>, áp dụng cho ô
    /// Giờ:Phút của Thời gian bắt đầu.
    /// </summary>
    private void StartTimeOfDayTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            textBox.Text = "00:00";
        }
    }

    /// <summary>
    /// US-05 AC9 (=US-21a AC4/AC9): rời khỏi ô "Lot" -> tra cứu ngay (không cần rời màn hình) để gợi ý "Tổng SL
    /// Lot" hiện có + breakdown đã chạy OK, hoặc đánh dấu Lot hoàn toàn mới (AC7).
    /// </summary>
    private async void LotTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadLotInfoCommand.ExecuteAsync(null);
    }
}
