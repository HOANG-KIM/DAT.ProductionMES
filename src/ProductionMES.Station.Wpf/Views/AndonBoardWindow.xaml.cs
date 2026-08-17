using System.Windows;
using System.Windows.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Services.Navigation;

namespace ProductionMES.Station.Wpf.Views;

/// <summary>
/// Cửa sổ Andon Board (ADR-006) — fullscreen, luôn tồn tại suốt vòng đời ứng dụng. Nội dung dashboard thật
/// (PLAN/ACTUAL/BALANCE, popup scan) thuộc US-07/US-09, chưa triển khai ở đây — chỉ dựng khung cửa sổ + hành vi
/// điều hướng (Esc) theo đúng ADR-006 để làm nền cho US-05/US-05a/US-05b.
/// </summary>
public partial class AndonBoardWindow : Window
{
    private readonly IWindowCoordinator _coordinator;

    public AndonBoardWindow(IWindowCoordinator coordinator, StationOptions options)
    {
        InitializeComponent();
        _coordinator = coordinator;
        StationLabel.Text = options.WorkStationName;
        StageLabel.Text = options.StageName.ToUpperInvariant();

        // Không có nút đóng thật (WindowStyle=None) — chặn Alt+F4 vô tình tắt board luôn hiển thị cho công nhân.
        Closing += (_, e) => e.Cancel = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        // Andon Board hiện chưa có popup scan (US-07/US-09 chưa triển khai) nên Esc luôn chuyển thẳng sang Main
        // Screen — khi popup được thêm sau này, ưu tiên đóng popup trước như đã chốt ở ADR-006.
        _coordinator.ShowMainScreen();
        e.Handled = true;
    }
}
