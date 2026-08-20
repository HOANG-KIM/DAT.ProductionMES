using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>Trang "Cấu hình trạm" — Tổ trưởng sửa 1 phần <c>appsettings.json</c> qua UI thay vì sửa tay JSON.</summary>
public partial class StationConfigPage : Page
{
    private readonly StationConfigViewModel _viewModel;
    private readonly ISupervisorSessionService _sessionService;

    public StationConfigPage(StationConfigViewModel viewModel, ISupervisorSessionService sessionService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _sessionService = sessionService;
        DataContext = _viewModel;
        _viewModel.SavedSuccessfully += OnSavedSuccessfully;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
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

    /// <summary>
    /// Sau khi lưu file thành công: hiện <see cref="RestartPromptDialog"/> đúng Owner (Window chứa trang) để
    /// CenterOwner định vị chính xác — nếu Tổ trưởng chọn "Khởi động lại ngay", tự khởi động lại tiến trình rồi
    /// thoát ứng dụng hiện tại (StationOptions là singleton, không mutate live được — xem remarks ViewModel).
    /// </summary>
    private void OnSavedSuccessfully(object? sender, EventArgs e)
    {
        var dialog = new RestartPromptDialog
        {
            Owner = Window.GetWindow(this),
        };
        dialog.ShowDialog();

        if (dialog.RestartNow)
        {
            Process.Start(Process.GetCurrentProcess().MainModule!.FileName!);
            Application.Current.Shutdown();
        }
    }
}
