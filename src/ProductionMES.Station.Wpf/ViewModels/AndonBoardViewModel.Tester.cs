using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Tester.Station;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// Chế độ máy thông điện của trạm (US-11): quyết định <c>AndonBoardViewModel.HandleScanAsync</c> đi luồng nào.
/// </summary>
public enum TesterModeKind
{
    /// <summary>Chưa đọc được trạng thái từ server (khởi động / mất server) — CHẶN quét (US-11 AC11).</summary>
    Unknown = 0,

    /// <summary>Trạm không có máy (<c>UseArduino = false</c>) — luồng scan thường, không công tắc, không mở COM (US-11 AC1, US-14 AC2).</summary>
    NoMachine,

    /// <summary>Trạm có máy, công tắc TẮT — luồng scan thường nhưng server lưu <c>TesterMode = Bypass</c>; COM đóng (US-11 AC6/AC7).</summary>
    Bypass,

    /// <summary>Trạm có máy, công tắc BẬT — luồng "thông điện trước, quét QR sau" (US-12) qua <see cref="TesterStationController"/>.</summary>
    Active,
}

/// <summary>
/// Phần máy thông điện ESP32 của <see cref="AndonBoardViewModel"/> (US-11 công tắc/Bypass, US-12 luồng quét, US-14 kết nối):
/// chỉ báo kết nối, chữ trạng thái lớn, thông báo BIND_FAIL, "Hủy kết quả", "Thử lại", công tắc chế độ + lý do, banner
/// mất focus. Mọi quyết định nghiệp vụ nằm ở <see cref="TesterStationController"/> (thư viện Station.Tester, test được).
/// </summary>
public partial class AndonBoardViewModel
{
    /// <summary>Permission đổi công tắc — literal khớp <c>PermissionPolicies.TesterModeChange</c> phía server (Station.Wpf KHÔNG reference backend).</summary>
    internal const string TesterModeChangePermission = "TesterMode.Change";

    /// <summary>Permission "Hủy kết quả" (DISCARD) — literal khớp <c>PermissionPolicies.TesterResultDiscard</c> phía server.</summary>
    internal const string TesterResultDiscardPermission = "TesterResult.Discard";

    private static readonly TimeSpan TesterNoticeInfoHide = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan TesterNoticeErrorHide = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan TesterModeRetryInterval = TimeSpan.FromSeconds(5);

    /// <summary>Gợi ý lý do khi TẮT máy (US-11 AC4: gợi ý + cho tự gõ, giống lý do Scan NG).</summary>
    private static readonly string[] TesterModeOffReasons =
    {
        "Máy thông điện hỏng",
        "Đang sửa / bảo trì máy thông điện",
        "Đang học lại mẫu (Learn) trên máy",
        "Lỗi kết nối cổng COM / cáp USB",
    };

    /// <summary>Gợi ý lý do khi BẬT lại máy.</summary>
    private static readonly string[] TesterModeOnReasons =
    {
        "Máy thông điện đã sửa xong",
        "Hoàn tất bảo trì / học mẫu",
        "Bật lại theo quy trình",
    };

    private DispatcherTimer _testerNoticeTimer = null!;
    private DispatcherTimer _testerModeRetryTimer = null!;
    private string? _testerModeAccessToken;
    private bool _testerModeLoading;
    private TesterState _lastTesterState = TesterState.Stopped;

    // ---------------------------------------------------------------------------------------------------------
    // Chế độ máy thông điện của trạm (US-11)
    // ---------------------------------------------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTesterModeUnknown))]
    [NotifyPropertyChangedFor(nameof(HasTesterMachine))]
    [NotifyPropertyChangedFor(nameof(IsTesterActive))]
    [NotifyPropertyChangedFor(nameof(IsTesterSwitchOn))]
    [NotifyPropertyChangedFor(nameof(TesterSwitchLabel))]
    private TesterModeKind testerModeKind = TesterModeKind.Unknown;

    public bool IsTesterModeUnknown => TesterModeKind == TesterModeKind.Unknown;

    /// <summary>"Trạm có máy thông điện" (<c>UseArduino</c>) — chỉ khi đó mới hiện công tắc chế độ (US-11 AC1/AC3).</summary>
    public bool HasTesterMachine => TesterModeKind is TesterModeKind.Bypass or TesterModeKind.Active;

    /// <summary>Có máy + công tắc BẬT: luồng US-12, hiện panel trạng thái máy.</summary>
    public bool IsTesterActive => TesterModeKind == TesterModeKind.Active;

    public bool IsTesterSwitchOn => IsTesterActive;

    public string TesterSwitchLabel => IsTesterActive ? "MÁY THÔNG ĐIỆN: BẬT" : "MÁY THÔNG ĐIỆN: TẮT";

    /// <summary>Lý do không xác định được chế độ (US-11 AC11) — hiển thị ở banner chặn quét.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTesterModeError))]
    private string testerModeError = string.Empty;

    public bool HasTesterModeError => !string.IsNullOrEmpty(TesterModeError);

    /// <summary>
    /// US-12 AC18 (spec-app 6.2): cửa sổ Andon không được chọn (<c>Deactivated</c>) — máy quét HID chỉ gõ vào cửa sổ đang
    /// focus nên mã quét lúc này sẽ vào cửa sổ khác. Áp dụng mọi trạm. Đặt từ code-behind qua <see cref="SetWindowActive"/>.
    /// </summary>
    [ObservableProperty]
    private bool isWindowInactive;

    /// <summary>Gọi từ <c>AndonBoardWindow</c> khi Activated/Deactivated.</summary>
    public void SetWindowActive(bool active) => IsWindowInactive = !active;

    // ---------------------------------------------------------------------------------------------------------
    // Trạng thái máy phản chiếu từ TesterStationController (US-12/US-14 AC9)
    // ---------------------------------------------------------------------------------------------------------

    [ObservableProperty]
    private string testerStatusText = string.Empty;

    [ObservableProperty]
    private Brush testerStatusBrush = Brushes.White;

    [ObservableProperty]
    private bool isTesterConnected;

    /// <summary>Banner đỏ nhấp nháy "MẤT KẾT NỐI MÁY THÔNG ĐIỆN" (US-14 AC3).</summary>
    [ObservableProperty]
    private bool isTesterLinkLost;

    /// <summary>Banner "Máy chưa có mẫu — cần Learn" (US-14 AC8) — không chặn cổng.</summary>
    [ObservableProperty]
    private bool testerNoSample;

    /// <summary>"Máy: KẾT NỐI · FW · GM" (US-14 AC9).</summary>
    [ObservableProperty]
    private string testerMachineInfo = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DiscardTesterResultCommand))]
    private bool canDiscardTesterResult;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetryTesterSaveCommand))]
    private bool canRetryTesterSave;

    // ---------------------------------------------------------------------------------------------------------
    // Thông báo cho người vận hành (BIND_FAIL, rút dây, ...) — không âm thanh
    // ---------------------------------------------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTesterNotice))]
    private string testerNoticeText = string.Empty;

    public bool HasTesterNotice => !string.IsNullOrEmpty(TesterNoticeText);

    [ObservableProperty]
    private Brush testerNoticeBrush = Brushes.White;

    /// <summary>true: thông báo lỗi giữ tới hết thời gian dài hoặc tới khi bấm "OK" (US-12 AC7).</summary>
    [ObservableProperty]
    private bool testerNoticeNeedsAck;

    // ---------------------------------------------------------------------------------------------------------
    // Overlay đổi công tắc + lý do (US-11 AC4/AC5)
    // ---------------------------------------------------------------------------------------------------------

    [ObservableProperty]
    private bool isTesterModeOverlayVisible;

    [ObservableProperty]
    private string testerModeOverlayTitle = string.Empty;

    [ObservableProperty]
    private string testerModeOverlayHint = string.Empty;

    [ObservableProperty]
    private string testerModeReasonText = string.Empty;

    public ObservableCollection<string> TesterModeReasonSuggestions { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTesterModeOverlayError))]
    private string testerModeOverlayError = string.Empty;

    public bool HasTesterModeOverlayError => !string.IsNullOrEmpty(TesterModeOverlayError);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmTesterModeChangeCommand))]
    private bool isTesterModeOverlayBusy;

    // ---------------------------------------------------------------------------------------------------------
    // Khởi tạo + đọc chế độ từ server
    // ---------------------------------------------------------------------------------------------------------

    private void InitializeTester()
    {
        _tester.Changed += OnTesterChanged;
        _tester.NoticeRaised += OnTesterNotice;
        _tester.ScanSaved += OnTesterScanSaved;

        _testerNoticeTimer = new DispatcherTimer { Interval = TesterNoticeInfoHide };
        _testerNoticeTimer.Tick += (_, _) => ClearTesterNotice();

        // US-11 AC11: chưa đọc được trạng thái -> thử lại liên tục (5s) cho tới khi đọc được; sau đó RefreshAsync định kỳ lo.
        _testerModeRetryTimer = new DispatcherTimer { Interval = TesterModeRetryInterval };
        _testerModeRetryTimer.Tick += async (_, _) =>
        {
            if (IsTesterModeUnknown)
            {
                await RefreshTesterModeAsync();
            }
        };
        _testerModeRetryTimer.Start();
    }

    /// <summary>
    /// US-11 AC10/AC11: đọc <c>UseArduino</c> + công tắc từ server (nguồn sự thật duy nhất). Đọc lỗi khi chưa biết chế độ ->
    /// giữ <see cref="TesterModeKind.Unknown"/> (chặn quét); đọc lỗi khi đã biết -> giữ nguyên chế độ hiện tại.
    /// </summary>
    public async Task RefreshTesterModeAsync()
    {
        if (_testerModeLoading)
        {
            return;
        }

        _testerModeLoading = true;
        try
        {
            var state = await _testerApiClient.GetStationStateAsync();
            ApplyTesterModeState(state.UseArduino, state.TesterEnabled);
            TesterModeError = string.Empty;
        }
        catch (Exception ex)
        {
            if (IsTesterModeUnknown)
            {
                TesterModeError = ex switch
                {
                    ApiException api => api.Message,
                    HttpRequestException http => NetworkErrorMessage.ForConnectionFailure(http),
                    TaskCanceledException => NetworkErrorMessage.ForTimeout(),
                    _ => ex.Message,
                };
            }
        }
        finally
        {
            _testerModeLoading = false;
        }
    }

    /// <summary>Áp dụng trạng thái server: mở/đóng cổng COM đúng theo chế độ (US-11 AC1/AC7/AC8, US-14 AC2).</summary>
    private void ApplyTesterModeState(bool useArduino, bool testerEnabled)
    {
        var newKind = !useArduino ? TesterModeKind.NoMachine : testerEnabled ? TesterModeKind.Active : TesterModeKind.Bypass;
        if (newKind == TesterModeKind)
        {
            return;
        }

        // Đang có kết quả chờ (WaitScan/Binding/Saving/Discarding): hoãn tới lần đọc sau, không bỏ rơi kết quả của máy.
        if (IsTesterActive && !_tester.CanChangeMode)
        {
            return;
        }

        TesterModeKind = newKind;
        if (newKind == TesterModeKind.Active)
        {
            _tester.Start();
        }
        else
        {
            _tester.Stop();
        }

        SyncTesterState();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Phản chiếu TesterStationController
    // ---------------------------------------------------------------------------------------------------------

    private void OnTesterChanged()
    {
        var state = _tester.State;
        if (state == TesterState.WaitScan && _lastTesterState == TesterState.Ready)
        {
            // Vừa có kết quả OK mới: bỏ thông báo cũ ("Chưa thông điện"...) để chữ "QUÉT MÃ QR" nổi bật.
            ClearTesterNotice();
        }

        _lastTesterState = state;
        SyncTesterState();
    }

    private void SyncTesterState()
    {
        var active = IsTesterActive;
        TesterStatusText = active ? _tester.StatusText : string.Empty;
        TesterStatusBrush = active ? BrushForTesterState() : Brushes.White;
        IsTesterConnected = active && _tester.IsConnected;
        IsTesterLinkLost = active && _tester.State == TesterState.Disconnected;
        TesterNoSample = active && _tester.IsConnected && _tester.NoSample;
        TesterMachineInfo = active ? BuildMachineInfo() : string.Empty;
        CanDiscardTesterResult = active && _tester.CanDiscard;
        CanRetryTesterSave = active && _tester.CanRetrySave;
    }

    private string BuildMachineInfo()
    {
        if (!_tester.IsConnected)
        {
            return "Máy: MẤT KẾT NỐI";
        }

        var gm = string.IsNullOrEmpty(_tester.GoldenCrc) ? "----" : _tester.GoldenCrc;
        var fw = string.IsNullOrEmpty(_tester.Firmware) ? "?" : _tester.Firmware;
        var pins = _tester.Pins is { } p ? $" · {p} pin" : string.Empty;
        return $"Máy: KẾT NỐI · FW {fw} · GM {gm}{pins}";
    }

    private Brush BrushForTesterState() => _tester.State switch
    {
        TesterState.WaitScan => FreezeBrush("#4CAF50"),
        TesterState.Disconnected => FreezeBrush("#E53935"),
        TesterState.Saving when _tester.CanRetrySave => FreezeBrush("#E53935"),
        TesterState.Binding or TesterState.Saving or TesterState.Discarding => FreezeBrush("#FFC107"),
        _ => FreezeBrush("#D5DAE3"),
    };

    private void OnTesterScanSaved(string tagCode, object? payload)
    {
        // Thùng đóng gói (nếu trạm này đồng thời là công đoạn Đóng thùng): cập nhật bộ đếm + tự in như luồng thường.
        if (payload is ScanResultDto dto && dto.IsPackingStage)
        {
            ApplyPackingScanResult(dto);
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Thông báo cho người vận hành
    // ---------------------------------------------------------------------------------------------------------

    private enum TesterNoticeSeverity
    {
        Info,
        Success,
        Error,
    }

    private void OnTesterNotice(TesterNotice notice)
    {
        switch (notice.Kind)
        {
            // Đã có banner đỏ + chỉ báo kết nối riêng — không lặp lại thành thông báo.
            case TesterNoticeKind.LinkLost:
            case TesterNoticeKind.LinkRestored:
                return;

            case TesterNoticeKind.Saved:
                ShowTesterNotice(TesterNoticeSeverity.Success, notice.Message);
                return;

            case TesterNoticeKind.WireNg:
            case TesterNoticeKind.NoResponse:
            case TesterNoticeKind.MachineError:
            case TesterNoticeKind.SaveFailed:
                ShowTesterNotice(TesterNoticeSeverity.Error, notice.Message);
                return;

            default:
                ShowTesterNotice(TesterNoticeSeverity.Info, notice.Message);
                return;
        }
    }

    private void ShowTesterNotice(TesterNoticeSeverity severity, string message)
    {
        TesterNoticeText = message;
        TesterNoticeNeedsAck = severity == TesterNoticeSeverity.Error;
        TesterNoticeBrush = severity switch
        {
            TesterNoticeSeverity.Success => FreezeBrush("#4CAF50"),
            TesterNoticeSeverity.Error => FreezeBrush("#FF6B63"),
            _ => FreezeBrush("#FFC107"),
        };

        _testerNoticeTimer.Stop();
        _testerNoticeTimer.Interval = severity == TesterNoticeSeverity.Error ? TesterNoticeErrorHide : TesterNoticeInfoHide;
        _testerNoticeTimer.Start();
    }

    private void ClearTesterNotice()
    {
        _testerNoticeTimer.Stop();
        TesterNoticeText = string.Empty;
        TesterNoticeNeedsAck = false;
    }

    [RelayCommand]
    private void DismissTesterNotice() => ClearTesterNotice();

    // ---------------------------------------------------------------------------------------------------------
    // Quét tem ở chế độ máy (US-12)
    // ---------------------------------------------------------------------------------------------------------

    private async Task HandleTesterScanAsync(string tagCode)
    {
        await _scanLock.WaitAsync();
        try
        {
            _autoCloseTimer.Stop();
            ClearTesterNotice();

            var outcome = await _tester.HandleScanAsync(tagCode);
            switch (outcome.Kind)
            {
                case TesterScanOutcomeKind.Blocked:
                    // US-14 AC3: mất kết nối -> chặn quét, không gửi BIND, không lưu gì.
                    ShowTesterNotice(TesterNoticeSeverity.Error, outcome.Message);
                    break;

                case TesterScanOutcomeKind.Busy:
                    ShowTesterNotice(TesterNoticeSeverity.Info, outcome.Message);
                    break;

                case TesterScanOutcomeKind.Rejected:
                    // US-12 AC3: tem bị FR-08 từ chối -> KHÔNG BIND, máy vẫn khóa; banner Lưu/Thoát của US-27 như cũ.
                    _pendingRejectedScanResult = outcome.RejectionPayload as ScanResultDto;
                    ShowRejectBanner(tagCode, string.IsNullOrEmpty(outcome.Message) ? "Scan bị từ chối." : outcome.Message);
                    break;

                case TesterScanOutcomeKind.ServerError:
                    ShowErrorBanner(tagCode, outcome.Message);
                    break;

                case TesterScanOutcomeKind.Aborted:
                    ShowTesterNotice(TesterNoticeSeverity.Error, outcome.Message);
                    break;

                // SentToMachine / Bound: phản hồi hiển thị bất đồng bộ qua OnTesterNotice / TesterStatusText.
            }
        }
        finally
        {
            _scanLock.Release();
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Hủy kết quả / Thử lại (US-12 AC11/AC13)
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>"Hủy kết quả": cần Tổ trưởng (permission <c>TesterResult.Discard</c>); sai tài khoản/thiếu quyền/Hủy -> KHÔNG gửi DISCARD.</summary>
    [RelayCommand(CanExecute = nameof(CanDiscardTesterResult))]
    private void DiscardTesterResult()
    {
        if (!_tester.CanDiscard)
        {
            return;
        }

        var dialog = _serviceProvider.GetRequiredService<Views.LoginDialog>();
        dialog.ViewModel.RequiredPermission = TesterResultDiscardPermission;
        dialog.Owner = Application.Current?.MainWindow;

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var approver = dialog.ViewModel.NgConfirmationLoginResult?.Username;
        if (string.IsNullOrEmpty(approver))
        {
            return;
        }

        if (!_tester.Discard(approver))
        {
            ShowTesterNotice(TesterNoticeSeverity.Error, "Lúc này không hủy được kết quả (trạng thái máy đã thay đổi).");
        }
    }

    /// <summary>"Thử lại" sau khi lưu lên server lỗi / chưa nhận UNLOCKED.</summary>
    [RelayCommand(CanExecute = nameof(CanRetryTesterSave))]
    private void RetryTesterSave()
    {
        ClearTesterNotice();
        _tester.RetrySave();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Công tắc "Đang sử dụng máy thông điện" (US-11 AC3-AC9)
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Bấm công tắc: (1) AC9 — đang có kết quả chờ thì từ chối, không cần đăng nhập; (2) đăng nhập nâng quyền
    /// (<c>TesterMode.Change</c>) — sai tài khoản/thiếu quyền/Hủy => giữ nguyên, không log (AC5); (3) nhập lý do bắt buộc.
    /// </summary>
    [RelayCommand]
    private void ToggleTesterMode()
    {
        if (!HasTesterMachine)
        {
            return;
        }

        if (!_tester.CanChangeMode)
        {
            ShowTesterNotice(TesterNoticeSeverity.Error, "Hủy kết quả đang chờ trước khi đổi chế độ.");
            return;
        }

        var dialog = _serviceProvider.GetRequiredService<Views.LoginDialog>();
        dialog.ViewModel.RequiredPermission = TesterModeChangePermission;
        dialog.Owner = Application.Current?.MainWindow;

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _testerModeAccessToken = dialog.ViewModel.NgConfirmationLoginResult?.AccessToken;
        if (string.IsNullOrEmpty(_testerModeAccessToken))
        {
            return;
        }

        // Trạng thái có thể đổi trong lúc nhập mật khẩu (vd máy vừa trả RESULT).
        if (!_tester.CanChangeMode)
        {
            _testerModeAccessToken = null;
            ShowTesterNotice(TesterNoticeSeverity.Error, "Hủy kết quả đang chờ trước khi đổi chế độ.");
            return;
        }

        var turningOn = !IsTesterActive;
        TesterModeOverlayTitle = turningOn ? "BẬT MÁY THÔNG ĐIỆN" : "TẮT MÁY THÔNG ĐIỆN";
        TesterModeOverlayHint = turningOn
            ? "App sẽ mở cổng COM và chạy theo luồng \"thông điện trước, quét QR sau\"."
            : "Quét QR sẽ lưu thẳng (đánh dấu Bypass), không qua máy thông điện. App đóng cổng COM, máy tự chạy độc lập sau tối đa 5 giây.";

        TesterModeReasonSuggestions.Clear();
        foreach (var reason in turningOn ? TesterModeOnReasons : TesterModeOffReasons)
        {
            TesterModeReasonSuggestions.Add(reason);
        }

        TesterModeReasonText = string.Empty;
        TesterModeOverlayError = string.Empty;
        IsTesterModeOverlayVisible = true;
    }

    private bool CanConfirmTesterModeChange() => !IsTesterModeOverlayBusy;

    /// <summary>Xác nhận đổi công tắc với lý do đã nhập. Lỗi server -> báo lỗi, KHÔNG đổi (AC11).</summary>
    [RelayCommand(CanExecute = nameof(CanConfirmTesterModeChange))]
    private async Task ConfirmTesterModeChangeAsync()
    {
        var reason = TesterModeReasonText.Trim();
        if (reason.Length == 0)
        {
            TesterModeOverlayError = "Phải chọn hoặc nhập lý do đổi chế độ.";
            return;
        }

        if (string.IsNullOrEmpty(_testerModeAccessToken))
        {
            TesterModeOverlayError = "Thiếu thông tin đăng nhập Tổ trưởng — vui lòng bấm lại công tắc.";
            return;
        }

        if (!_tester.CanChangeMode)
        {
            TesterModeOverlayError = "Hủy kết quả đang chờ trước khi đổi chế độ.";
            return;
        }

        var turningOn = !IsTesterActive;
        IsTesterModeOverlayBusy = true;
        TesterModeOverlayError = string.Empty;
        try
        {
            var state = await _testerApiClient.SetEnabledAsync(turningOn, reason, _testerModeAccessToken);
            CloseTesterModeOverlay();
            ApplyTesterModeState(state.UseArduino, state.TesterEnabled);
            ShowTesterNotice(
                TesterNoticeSeverity.Info,
                state.TesterEnabled ? "Đã BẬT máy thông điện." : "Đã TẮT máy thông điện — quét QR lưu thẳng (Bypass).");
        }
        catch (ApiException ex)
        {
            TesterModeOverlayError = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            TesterModeOverlayError = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            TesterModeOverlayError = NetworkErrorMessage.ForTimeout();
        }
        finally
        {
            IsTesterModeOverlayBusy = false;
        }
    }

    [RelayCommand]
    private void CancelTesterModeOverlay() => CloseTesterModeOverlay();

    private void CloseTesterModeOverlay()
    {
        IsTesterModeOverlayVisible = false;
        _testerModeAccessToken = null;
        TesterModeReasonText = string.Empty;
        TesterModeOverlayError = string.Empty;
    }
}
