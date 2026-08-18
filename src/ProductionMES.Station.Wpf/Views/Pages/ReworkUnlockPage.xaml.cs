using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>Trang "Mở khóa rework" (US-19 AC2/AC6) — Tổ trưởng nhập/scan mã tem bị NG để mở khóa cho phép scan lại.</summary>
public partial class ReworkUnlockPage : Page
{
    private readonly ReworkUnlockViewModel _viewModel;

    public ReworkUnlockPage(ReworkUnlockViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToHome();
    }

    /// <summary>
    /// Scan/nhập mã tem xong -> Enter (cùng kiểu bắt input máy scan HID như <c>AndonBoardWindow.ScanInputBox_KeyDown</c>)
    /// -> tự tra cứu lỗi NG gần nhất luôn, không bắt buộc bấm nút "Tra cứu lỗi" (feedback 18/08/2026).
    /// </summary>
    private void TagCodeTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        if (_viewModel.LookupCommand.CanExecute(null))
        {
            _viewModel.LookupCommand.Execute(null);
        }
    }
}
