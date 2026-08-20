using System.Windows;
using ProductionMES.Station.Wpf.Views;

namespace ProductionMES.Station.Wpf.Services.Navigation;

/// <inheritdoc cref="IWindowCoordinator"/>
public class WindowCoordinator : IWindowCoordinator
{
    private Window? _andonBoardWindow;
    private Window? _mainWindow;

    public void Register(Window andonBoardWindow, Window mainWindow)
    {
        _andonBoardWindow = andonBoardWindow;
        _mainWindow = mainWindow;
    }

    public void ShowAndonBoard()
    {
        if (_andonBoardWindow is null)
        {
            return;
        }

        _andonBoardWindow.Show();
        _andonBoardWindow.Activate();
    }

    public void ShowMainScreen()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    public void ExitApplication()
    {
        // Cả AndonBoardWindow lẫn MainWindow đều tự chặn Closing (không có nút đóng thật/nút X đã ẩn) — gỡ khóa
        // CẢ 2 trước khi Shutdown(), không chỉ AndonBoardWindow: Shutdown() đóng lần lượt từng Window, nếu
        // MainWindow vẫn tự hủy Close (Cancel=true) giữa chừng, nó sẽ cố gọi lại ShowAndonBoard() trong khi
        // AndonBoardWindow có thể đã đóng xong ở bước trước → Show() trên Window đã đóng ném
        // InvalidOperationException, đồng thời khiến Shutdown() bị huỷ giữa chừng (WPF hủy toàn bộ shutdown nếu
        // bất kỳ Window nào hủy Closing trong lúc đó).
        if (_andonBoardWindow is AndonBoardWindow andonBoardWindow)
        {
            andonBoardWindow.AllowExit();
        }

        if (_mainWindow is MainWindow mainWindow)
        {
            mainWindow.AllowExit();
        }

        Application.Current.Shutdown();
    }
}
