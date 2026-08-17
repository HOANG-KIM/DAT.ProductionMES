using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ProductionMES.Station.Wpf.Services.Navigation;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views;

/// <summary>
/// Cửa sổ Andon Board (ADR-006) — fullscreen, luôn tồn tại suốt vòng đời ứng dụng. Triển khai luồng scan cơ bản
/// (US-07 AC2-AC5): bắt input máy scan HID qua <see cref="ScanInputBox"/> (ẩn, luôn giữ focus), gọi
/// <see cref="AndonBoardViewModel.HandleScanAsync"/>, hiển thị banner OK/NG qua binding; và bảng PLAN/ACTUAL/
/// BALANCE theo mốc giờ (US-09 AC1-AC6), tải qua <see cref="AndonBoardViewModel.InitializeAsync"/>.
/// </summary>
public partial class AndonBoardWindow : Window
{
    private readonly IWindowCoordinator _coordinator;
    private readonly AndonBoardViewModel _viewModel;

    public AndonBoardWindow(IWindowCoordinator coordinator, AndonBoardViewModel viewModel)
    {
        InitializeComponent();
        _coordinator = coordinator;
        _viewModel = viewModel;
        DataContext = viewModel;

        // Không có nút đóng thật (WindowStyle=None) — chặn Alt+F4 vô tình tắt board luôn hiển thị cho công nhân.
        Closing += (_, e) => e.Cancel = true;

        // US-09: luôn tự cuộn xuống dòng cuối (dòng "hiện tại") mỗi khi Rows đổi (làm mới định kỳ hoặc scan mới),
        // không bắt operator phải tự kéo cuộn để thấy số liệu mới nhất. Dispatcher.BeginInvoke ở Background
        // priority để chạy SAU khi layout đã tính lại ScrollableHeight theo nội dung mới (gọi ngay trong sự kiện
        // CollectionChanged thì ScrollableHeight vẫn còn là giá trị cũ, cuộn sai chỗ).
        _viewModel.Rows.CollectionChanged += (_, _) =>
            Dispatcher.BeginInvoke(() => RowsScrollViewer.ScrollToBottom(), DispatcherPriority.Background);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FocusScanInput();

        // US-09: tải dữ liệu bảng PLAN/ACTUAL/BALANCE lần đầu ngay khi Window sẵn sàng, không chờ tick đầu tiên
        // của chu kỳ làm mới định kỳ (AndonBoardViewModel._boardRefreshTimer). Lỗi mạng/server tạm thời đã được
        // nuốt bên trong InitializeAsync/RefreshBoardAsync (không làm gián đoạn luồng scan).
        _ = _viewModel.InitializeAsync();
    }

    private void Window_Activated(object sender, EventArgs e) => FocusScanInput();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        // Ưu tiên đóng banner scan (nếu đang mở) trước khi chuyển Main Screen, đúng thứ tự Esc 1 chiều (ADR-006).
        if (_viewModel.IsBannerVisible && _viewModel.RequiresAcknowledgement)
        {
            _viewModel.AcknowledgeBannerCommand.Execute(null);
            e.Handled = true;
            return;
        }

        _coordinator.ShowMainScreen();
        e.Handled = true;
    }

    /// <summary>
    /// Bắt input máy scan HID (keyboard-wedge): ký tự tem đã tự tích lũy vào <see cref="ScanInputBox"/> như 1
    /// TextBox bình thường, Enter kết thúc 1 lượt scan. Độc lập với <see cref="Window_PreviewKeyDown"/> (chỉ xử
    /// lý Esc) — không tranh chấp phím với luồng điều hướng.
    /// </summary>
    private void ScanInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        var tagCode = ScanInputBox.Text.Trim();
        ScanInputBox.Clear();

        if (tagCode.Length > 0)
        {
            _ = _viewModel.HandleScanAsync(tagCode);
        }
    }

    /// <summary>
    /// Ô nhập luôn phải giữ focus để không bỏ lỡ ký tự scan tiếp theo — bấm nút "Đã đọc, đóng" (hoặc bất kỳ
    /// control nào khác) làm mất focus thì trả lại ngay sau khi xử lý xong. Ngoại lệ duy nhất (US-07, chế độ
    /// nhập tay/test): nếu focus vừa chuyển sang <see cref="ManualScanInputBox"/> (chỉ có thể khi
    /// <c>EnableManualScanInput = true</c>, vì lúc đó ô này mới hiển thị/nhận focus được), KHÔNG cướp lại focus
    /// — để tester gõ tay được, không bị bật lại vào ô ẩn ngay khi vừa click vào. Không ảnh hưởng hành vi hiện
    /// có khi cờ tắt, vì khi đó <see cref="ManualScanInputBox"/> luôn <c>Collapsed</c>, không thể nhận focus.
    /// </summary>
    private void ScanInputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(Keyboard.FocusedElement, ManualScanInputBox))
        {
            return;
        }

        Dispatcher.BeginInvoke(FocusScanInput, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void AcknowledgeButton_Click(object sender, RoutedEventArgs e) => FocusScanInput();

    /// <summary>
    /// US-07, chế độ nhập tay/test: Enter trong ô nhập tay cũng kết thúc 1 lượt gửi, tương tự
    /// <see cref="ScanInputBox_KeyDown"/> — gọi <see cref="AndonBoardViewModel.SubmitManualScanCommand"/> (dùng
    /// lại đúng <see cref="AndonBoardViewModel.HandleScanAsync"/>, không xử lý kết quả riêng).
    /// </summary>
    private void ManualScanInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        _viewModel.SubmitManualScanCommand.Execute(null);
    }

    /// <summary>Sau khi bấm "Gửi", trả focus lại ô nhập tay để tester nhập tiếp mã tem kế tiếp không cần bấm chuột lại.</summary>
    private void ManualScanSubmitButton_Click(object sender, RoutedEventArgs e)
    {
        ManualScanInputBox.Focus();
        Keyboard.Focus(ManualScanInputBox);
    }

    private void FocusScanInput()
    {
        ScanInputBox.Focus();
        Keyboard.Focus(ScanInputBox);
    }
}
