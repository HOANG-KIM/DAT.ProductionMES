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

    /// <summary>true chỉ khi <see cref="AllowExit"/> đã được gọi (qua nút "Thoát ứng dụng" ở Trang chủ, xem
    /// <c>WindowCoordinator.ExitApplication</c>) — mọi lần Closing khác (Alt+F4, đóng nhầm) vẫn bị chặn.</summary>
    private bool _exitAllowed;

    public AndonBoardWindow(IWindowCoordinator coordinator, AndonBoardViewModel viewModel)
    {
        InitializeComponent();
        _coordinator = coordinator;
        _viewModel = viewModel;
        DataContext = viewModel;

        // Không có nút đóng thật (WindowStyle=None) — chặn Alt+F4 vô tình tắt board luôn hiển thị cho công nhân.
        // Chỉ mở khóa khi _exitAllowed = true (xem AllowExit), đúng luồng "Thoát ứng dụng" chủ động.
        Closing += (_, e) => e.Cancel = !_exitAllowed;

        // US-09: luôn tự cuộn xuống dòng cuối (dòng "hiện tại") mỗi khi Rows đổi (làm mới định kỳ hoặc scan mới),
        // không bắt operator phải tự kéo cuộn để thấy số liệu mới nhất. Dispatcher.BeginInvoke ở Background
        // priority để chạy SAU khi layout đã tính lại ScrollableHeight theo nội dung mới (gọi ngay trong sự kiện
        // CollectionChanged thì ScrollableHeight vẫn còn là giá trị cũ, cuộn sai chỗ).
        _viewModel.Rows.CollectionChanged += (_, _) =>
            Dispatcher.BeginInvoke(() => RowsScrollViewer.ScrollToBottom(), DispatcherPriority.Background);

        // US-18 AC1/AC3/AC4: quản lý focus cho từng bước của Chế độ Scan NG bằng cách lắng nghe đúng property đổi
        // trạng thái, KHÔNG dựa vào ScanInputBox_LostFocus cướp lại tự động nữa (xem comment ở đó — bug 18/08/2026
        // phát hiện việc cướp focus giữa lúc đang bấm chuột làm WPF huỷ luôn Click/Command đang chờ xử lý).
        _viewModel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(AndonBoardViewModel.IsNgReasonPanelVisible) when _viewModel.IsNgReasonPanelVisible:
                    // Form nhập lý do vừa mở — chuyển hẳn keyboard focus sang ô nhập lý do (bàn phím thật, không
                    // phải máy scan).
                    Dispatcher.BeginInvoke(() =>
                    {
                        NgReasonComboBox.Focus();
                        Keyboard.Focus(NgReasonComboBox);
                    }, DispatcherPriority.Background);
                    break;

                case nameof(AndonBoardViewModel.IsNgReasonPanelVisible):
                    // Form nhập lý do vừa đóng (xác nhận/hủy) — trả lại focus cho ScanInputBox như bình thường.
                    FocusScanInput();
                    break;

                case nameof(AndonBoardViewModel.IsWaitingForNgTagCode) when _viewModel.IsWaitingForNgTagCode:
                    // Vừa kích hoạt/gia hạn Chế độ Scan NG, đang chờ quét tem lỗi — cần focus ScanInputBox để bắt
                    // input máy quét tiếp theo (nút "NG" vừa lấy focus lúc được bấm).
                    FocusScanInput();
                    break;

                // US-25 AC5/AC7: overlay nhập/sửa số thùng vừa mở/đóng — cùng idiom IsNgReasonPanelVisible ở trên.
                case nameof(AndonBoardViewModel.IsPackingBoxNoOverlayVisible) when _viewModel.IsPackingBoxNoOverlayVisible:
                    Dispatcher.BeginInvoke(() =>
                    {
                        PackingBoxNoInputBox.SelectAll();
                        PackingBoxNoInputBox.Focus();
                        Keyboard.Focus(PackingBoxNoInputBox);
                    }, DispatcherPriority.Background);
                    break;

                case nameof(AndonBoardViewModel.IsPackingBoxNoOverlayVisible):
                    FocusScanInput();
                    break;
            }
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FocusScanInput();

        // US-09: tải dữ liệu bảng PLAN/ACTUAL/BALANCE lần đầu ngay khi Window sẵn sàng, không chờ tick đầu tiên
        // của chu kỳ làm mới định kỳ (AndonBoardViewModel._boardRefreshTimer). Lỗi mạng/server tạm thời đã được
        // nuốt bên trong InitializeAsync/RefreshBoardAsync (không làm gián đoạn luồng scan).
        _ = _viewModel.InitializeAsync();
    }

    // US-18: không cướp focus khỏi ô nhập lý do NG khi Window được kích hoạt lại (vd Alt+Tab quay lại giữa lúc đang gõ).
    private void Window_Activated(object sender, EventArgs e)
    {
        if (!_viewModel.IsNgReasonPanelVisible)
        {
            FocusScanInput();
        }

        // US-25 (sửa lỗi 24/08/2026): Activated cũng là lúc quay lại từ MainWindow (US-05b "Chọn kế hoạch" ->
        // "Áp dụng" -> WindowCoordinator.ShowAndonBoard()) — tải lại ngay bảng PLAN/ACTUAL/BALANCE + trạng thái
        // đóng thùng thay vì chờ tick định kỳ tiếp theo (xem AndonBoardViewModel.RefreshAsync). Lỗi mạng/server
        // tạm thời đã được nuốt bên trong (không làm gián đoạn luồng scan).
        _ = _viewModel.RefreshAsync();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        // US-25 AC5/AC7: overlay nhập số thùng bắt đầu (bắt buộc — Esc KHÔNG được phép bỏ qua, chỉ chặn phím) /
        // sửa số thùng hiện tại (tùy chọn — Esc = Hủy, cùng idiom Chế độ Scan NG bên dưới).
        if (_viewModel.IsPackingBoxNoOverlayVisible)
        {
            if (_viewModel.IsEditingExistingBoxNo)
            {
                _viewModel.CancelPackingBoxNoOverlayCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        // US-18: đóng "popup" Chế độ Scan NG trước tiên (dù đang chờ quét tem hay đang nhập lý do), đúng thứ tự
        // Esc 1 chiều (ADR-006) — không lưu gì, quay về Chế độ Scan OK.
        if (_viewModel.IsNgModeActive)
        {
            _viewModel.CancelNgReasonCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // US-27 AC4: banner Lưu/Thoát (scan bị hệ thống tự động từ chối) — Esc = "Thoát" (không lưu gì), đúng thứ
        // tự Esc 1 chiều (ADR-006). Kiểm tra TRƯỚC RequiresAcknowledgement (US-18, banner "NG đã ghi nhận") vì 2
        // cờ này loại trừ nhau (không bao giờ cùng true).
        if (_viewModel.IsBannerVisible && _viewModel.RequiresRejectDecision)
        {
            _viewModel.ExitRejectedScanCommand.Execute(null);
            e.Handled = true;
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
    /// Ô nhập luôn phải giữ focus để không bỏ lỡ ký tự scan tiếp theo — mất focus vì lý do khác (vd Alt+Tab ra
    /// ngoài ứng dụng, hoặc 1 control không tương tác nào đó vô tình nhận focus) thì trả lại ngay.
    /// </summary>
    /// <remarks>
    /// BUG phát hiện 18/08/2026 (nút "NG" bấm không có phản hồi gì — US-18 AC1): bản trước đây cướp lại focus
    /// cho MỌI trường hợp mất focus (trừ <see cref="ManualScanInputBox"/>/<c>IsNgReasonPanelVisible</c>), kể cả
    /// khi mất focus vì người dùng đang BẤM CHUỘT vào 1 Button khác (vd nút "NG"). <see cref="Dispatcher"/> có
    /// thể chạy callback <c>Background</c> priority này ngay TRONG khoảng giữa MouseDown và MouseUp của cùng 1
    /// lượt click (2 message Win32 riêng biệt) — cướp lại keyboard focus giữa chừng khiến WPF huỷ luôn
    /// Click/Command đang chờ xử lý của Button đó (không throw exception, không log, im lặng như "click không
    /// phản hồi"). Fix: KHÔNG cướp lại focus nếu control vừa nhận focus là 1 control người dùng đang chủ động
    /// tương tác (Button/ComboBox/TextBox) — mỗi control đó tự chịu trách nhiệm trả lại focus cho
    /// <see cref="ScanInputBox"/> sau khi hoàn tất thao tác của nó (xem <see cref="AcknowledgeButton_Click"/>,
    /// <see cref="ManualScanSubmitButton_Click"/>, và <c>PropertyChanged</c> handler trong constructor cho
    /// <c>IsNgReasonPanelVisible</c>/<c>IsWaitingForNgTagCode</c>).
    /// </remarks>
    private void ScanInputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (Keyboard.FocusedElement is System.Windows.Controls.Primitives.ButtonBase
            or System.Windows.Controls.ComboBox
            or System.Windows.Controls.TextBox)
        {
            return;
        }

        Dispatcher.BeginInvoke(FocusScanInput, System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>US-18 AC3: Enter trong ô nhập lý do cũng xác nhận Scan NG luôn, không bắt buộc bấm nút "XÁC NHẬN NG".</summary>
    private void NgReasonComboBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        if (_viewModel.ConfirmNgReasonCommand.CanExecute(null))
        {
            _viewModel.ConfirmNgReasonCommand.Execute(null);
        }
    }

    private void AcknowledgeButton_Click(object sender, RoutedEventArgs e) => FocusScanInput();

    /// <summary>US-25 AC5/AC7: Enter trong ô nhập số thùng cũng xác nhận luôn, không bắt buộc bấm nút "XÁC NHẬN".</summary>
    private void PackingBoxNoInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        if (_viewModel.SubmitPackingBoxNoCommand.CanExecute(null))
        {
            _viewModel.SubmitPackingBoxNoCommand.Execute(null);
        }
    }

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

    /// <summary>Gỡ khóa Closing đúng 1 lần cho luồng "Thoát ứng dụng" chủ động — gọi từ <c>WindowCoordinator.ExitApplication</c>.</summary>
    public void AllowExit() => _exitAllowed = true;
}
