using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>
/// Trang "Cấu hình đóng gói theo Model" (US-24) — Tổ trưởng nâng quyền tại trạm quản lý CÙNG dữ liệu với
/// web-admin (AC6). Thao tác chọn file mẫu tem (upload) / chọn nơi lưu (download) dùng
/// <see cref="Microsoft.Win32.OpenFileDialog"/>/<see cref="Microsoft.Win32.SaveFileDialog"/> — thuần UI, đặt ở
/// code-behind thay vì ViewModel.
/// </summary>
public partial class PackingModelConfigPage : Page
{
    private readonly PackingModelConfigViewModel _viewModel;
    private readonly ISupervisorSessionService _sessionService;

    public PackingModelConfigPage(PackingModelConfigViewModel viewModel, ISupervisorSessionService sessionService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _sessionService = sessionService;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Nút "← Trang chủ" là tín hiệu "đã xong việc, chủ động rời khu vực nâng quyền" — clear session ngay trước
    /// khi điều hướng, không chờ idle timeout (cùng idiom LineStageSequencePage/PlanSettingsPage).
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

    private void LineStageSequenceButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToLineStageSequence();
    }

    /// <summary>AC4 — chọn file .xlsx từ máy cục bộ rồi tải lên (thay thế mẫu cũ nếu có).</summary>
    private async void UploadTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "File Excel (*.xlsx)|*.xlsx",
            Title = "Chọn file mẫu tem in",
        };

        if (dialog.ShowDialog() == true)
        {
            await _viewModel.UploadTemplateAsync(dialog.FileName);
        }
    }

    /// <summary>AC5 — chọn nơi lưu rồi tải xuống file mẫu tem đang cấu hình.</summary>
    private async void DownloadTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "File Excel (*.xlsx)|*.xlsx",
            Title = "Lưu file mẫu tem in",
            FileName = $"mau-tem-{_viewModel.Model}.xlsx",
        };

        if (dialog.ShowDialog() == true)
        {
            await _viewModel.DownloadTemplateAsync(dialog.FileName);
        }
    }
}
