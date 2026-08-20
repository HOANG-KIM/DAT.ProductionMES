using System.Windows;

namespace ProductionMES.Station.Wpf.Services.Navigation;

/// <summary>
/// Điều phối 2 cửa sổ <c>AndonBoardWindow</c>/<c>MainWindow</c> (ADR-006) mà không để 2 Window phụ thuộc vòng
/// lẫn nhau qua constructor (DI không resolve được circular dependency) — cả 2 chỉ phụ thuộc interface này,
/// instance thật của từng Window được gán vào sau khi cả 2 đã dựng xong (xem <c>App.xaml.cs</c>).
/// </summary>
public interface IWindowCoordinator
{
    void Register(Window andonBoardWindow, Window mainWindow);

    /// <summary>Chuyển sang Andon Board (đưa lên trước, không đóng Main Screen).</summary>
    void ShowAndonBoard();

    /// <summary>Chuyển sang Main Screen (đưa lên trước, không đóng Andon Board).</summary>
    void ShowMainScreen();

    /// <summary>
    /// Thoát hẳn ứng dụng — gỡ khóa Closing của <c>AndonBoardWindow</c> (mặc định luôn bị chặn, xem
    /// <c>AndonBoardWindow.xaml.cs</c>) rồi gọi <see cref="System.Windows.Application.Shutdown()"/>. Chỉ dùng
    /// cho đúng 1 nút "Thoát ứng dụng" ở Trang chủ — không dùng lại cho bất kỳ luồng nào khác.
    /// </summary>
    void ExitApplication();
}
