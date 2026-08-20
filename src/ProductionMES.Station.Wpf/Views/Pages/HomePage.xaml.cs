using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.Services.Navigation;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views.Pages;

/// <summary>
/// Trang chủ (Main Screen) — launcher tối giản: thông tin trạm + 4 tile thao tác vận hành/chất lượng (dùng
/// thường xuyên) + 1 nút nhỏ "Cấu hình trạm" cạnh dải thông tin TRẠM/LINE/CÔNG ĐOẠN (thiết lập hiếm khi đổi,
/// tách khỏi hàng tile chính để không đánh đồng trọng số với các thao tác dùng nhiều lần mỗi ca). Mỗi lối vào
/// chế độ Tổ trưởng yêu cầu đăng nhập Supervisor nếu chưa có phiên đang mở (ADR-005), rồi điều hướng nội bộ qua
/// <see cref="MainWindow"/>.
/// </summary>
public partial class HomePage : Page
{
    private readonly ISupervisorSessionService _sessionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowCoordinator _coordinator;

    public HomePage(HomeViewModel viewModel, ISupervisorSessionService sessionService, IServiceProvider serviceProvider, IWindowCoordinator coordinator)
    {
        InitializeComponent();
        DataContext = viewModel;
        _sessionService = sessionService;
        _serviceProvider = serviceProvider;
        _coordinator = coordinator;
    }

    private void PlanSettingsTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToPlanSettings();
        }
    }

    private void PlanSelectionTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToPlanSelection();
        }
    }

    private void LineStageSequenceTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToLineStageSequence();
        }
    }

    /// <summary>
    /// US-19 AC7 (18/08/2026): KHÔNG gọi <see cref="RequireAuth"/> (phiên đăng nhập Tổ trưởng dùng chung) như 3
    /// tile còn lại — chức năng "Mở khóa rework" bắt buộc đăng nhập lại riêng mỗi lần vào, xử lý ngay trong
    /// <c>ReworkUnlockViewModel.EnsureAuthenticated</c> (idiom re-auth-mỗi-lần của US-18 NG mode), không phải ở
    /// đây. Điều hướng thẳng vào trang, dù đang có phiên Tổ trưởng dùng chung hiệu lực hay không.
    /// </summary>
    private void ReworkUnlockTile_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Window.GetWindow(this)!).NavigateToReworkUnlock();
    }

    private void StationConfigTile_Click(object sender, RoutedEventArgs e)
    {
        if (RequireAuth())
        {
            ((MainWindow)Window.GetWindow(this)!).NavigateToStationConfig();
        }
    }

    private void BackToAndonButton_Click(object sender, RoutedEventArgs e)
    {
        _coordinator.ShowAndonBoard();
    }

    /// <summary>
    /// Thoát hẳn ứng dụng (đóng cả <c>AndonBoardWindow</c> lẫn <see cref="MainWindow"/>) — KHÔNG yêu cầu đăng
    /// nhập Tổ trưởng (quyết định 20/08/2026: ai cũng thoát được, ưu tiên tiện dụng cho IT/bảo trì/cuối ca hơn
    /// khóa kiosk tuyệt đối). Có hộp thoại xác nhận vì đây là hành động khó hoàn tác và ảnh hưởng cả xưởng (tắt
    /// hiển thị Andon Board) — bấm nhầm 1 cú vẫn cần thêm 1 bước xác nhận mới thực sự thoát.
    /// </summary>
    private void ExitApplicationButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Thoát ứng dụng sẽ tắt cả màn hình Andon Board — công nhân sẽ không còn xem được bảng PLAN/ACTUAL/BALANCE cho tới khi mở lại ứng dụng.\n\nBạn có chắc chắn muốn thoát?",
            "Xác nhận thoát ứng dụng",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result == MessageBoxResult.Yes)
        {
            _coordinator.ExitApplication();
        }
    }

    /// <summary>Trả về true nếu đã (hoặc vừa) đăng nhập Supervisor thành công — false nếu người dùng bấm Hủy ở dialog.</summary>
    private bool RequireAuth()
    {
        if (_sessionService.IsAuthenticated)
        {
            return true;
        }

        var dialog = _serviceProvider.GetRequiredService<Views.LoginDialog>();
        dialog.Owner = Window.GetWindow(this);
        var result = dialog.ShowDialog();
        return result == true;
    }
}
