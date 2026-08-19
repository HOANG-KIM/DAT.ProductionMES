using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.ReworkUnlocks;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Mở khóa rework" (US-19 AC2/AC6/AC7) — Tổ trưởng nhập/scan mã tem đang bị khóa do NG, xem lại
/// tại đúng công đoạn của trạm này (cố định theo <see cref="StationOptions"/>, giống <c>LineStageSequenceViewModel</c>
/// US-03 — Tổ trưởng KHÔNG chọn công đoạn khác từ màn này), nhập ghi chú tùy chọn rồi xác nhận mở khóa.
/// </summary>
/// <remarks>
/// **18/08/2026 (AC7 — bắt buộc re-auth mỗi lần)**: KHÔNG còn dùng phiên đăng nhập Tổ trưởng dùng chung
/// (<c>HomePage.RequireAuth</c>/<c>ISupervisorSessionService</c>, cách cũ giống <c>PlanSettingsPage</c>/
/// <c>LineStageSequencePage</c>) — danh tính đăng nhập vào chức năng này được dùng làm "Người sửa hàng" trong báo
/// cáo (US-21 AC11), phải đúng người thao tác của từng lượt. Áp dụng đúng idiom re-auth-mỗi-lần đã có ở US-18
/// (<c>AndonBoardViewModel.ActivateNgModeCommand</c>): <see cref="EnsureAuthenticated"/> hiển thị
/// <see cref="Views.LoginDialog"/> riêng với <c>RequiredPermission = "Scan.ReworkUnlock"</c>, lấy access token
/// dùng RIÊNG (KHÔNG ghi vào <c>ISupervisorSessionService</c>), gọi API qua <see cref="IReworkUnlockApiClient"/>
/// (đã đổi sang nhận token tường minh, xem ghi chú tại đó). Vì <c>ReworkUnlockPage</c>/<c>ReworkUnlockViewModel</c>
/// đăng ký Transient (mỗi lần điều hướng vào là 1 instance mới — xem <c>App.xaml.cs</c>), "rời màn hình" tự động
/// làm mất token đang giữ, thỏa đúng AC7 "vào lại chức năng này (kể cả cùng 1 phiên làm việc tại trạm) đều phải
/// đăng nhập lại". Riêng thao tác <see cref="UnlockAsync"/> (thao tác được audit, gắn danh tính "Người sửa hàng")
/// còn xóa token NGAY sau khi dùng (dù thành công hay lỗi) để đúng nghĩa "chỉ có hiệu lực 1 lần dùng" (FR-19) —
/// muốn mở khóa tem tiếp theo trong cùng 1 lần vào màn hình vẫn phải đăng nhập lại. <see cref="LookupAsync"/> (chỉ
/// mang tính tham khảo, KHÔNG phải thao tác được audit) tái dùng token đang còn hiệu lực nếu có, không tự xóa sau
/// khi tra cứu — quyết định kỹ thuật của dev để tránh bắt đăng nhập lại liên tục mỗi lần scan tem để xem thông
/// tin lỗi (UX), không ảnh hưởng tính đúng đắn của AC11 (Lookup không ghi dữ liệu).
/// </remarks>
public partial class ReworkUnlockViewModel : ObservableObject
{
    /// <summary>AC6/AC7: permission bắt buộc để hoàn tất đăng nhập — literal "Scan.ReworkUnlock" (Station.Wpf KHÔNG reference project backend, xem CLAUDE.md), phải khớp đúng policy phía server (US-19).</summary>
    internal const string ReworkUnlockPermission = "Scan.ReworkUnlock";

    private readonly IReworkUnlockApiClient _apiClient;
    private readonly StationOptions _options;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// AC7: access token đăng nhập RIÊNG cho lần vào chức năng này, gán bởi <see cref="EnsureAuthenticated"/> —
    /// KHÔNG ghi vào <see cref="Auth.ISupervisorSessionService"/> dùng chung. Xóa sau khi <see cref="UnlockAsync"/>
    /// dùng xong (single-use cho thao tác audit); giữ lại để <see cref="LookupAsync"/> có thể tái dùng.
    /// </summary>
    private string? _supervisorAccessToken;

    public string WorkStationName { get; }

    public string StageName { get; }

    [ObservableProperty]
    private string tagCode = string.Empty;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UnlockCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    /// <summary>true khi lần mở khóa gần nhất thành công — dùng để đổi màu <see cref="StatusMessage"/> (xanh) khác lỗi (đỏ) trên UI.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusBrush))]
    private bool isLastResultSuccess;

    /// <summary>Màu hiển thị <see cref="StatusMessage"/> — xanh (StatusOkBrush) khi thành công, đỏ (StatusNgBrush) khi lỗi.</summary>
    public Brush StatusBrush => (Brush)(Application.Current?.TryFindResource(IsLastResultSuccess ? "StatusOkBrush" : "StatusNgBrush") ?? Brushes.Red);

    // --- Tra cứu thông tin lỗi NG gần nhất (feedback 18/08/2026) — chỉ hiển thị tham khảo, KHÔNG chặn UnlockCommand. ---

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LookupCommand))]
    private bool isLookingUp;

    /// <summary>Thông báo lỗi khi tra cứu thất bại (vd mất mạng) — TÁCH RIÊNG khỏi <see cref="StatusMessage"/> (kết quả thao tác mở khóa).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLookupMessage))]
    private string lookupMessage = string.Empty;

    /// <summary>true khi <see cref="LookupMessage"/> có nội dung — điều khiển hiển thị dòng lỗi tra cứu trên UI.</summary>
    public bool HasLookupMessage => !string.IsNullOrEmpty(LookupMessage);

    /// <summary>true sau khi đã tra cứu thành công (dù tem chưa từng NG) — điều khiển hiển thị khu vực thông tin lỗi trên UI.</summary>
    [ObservableProperty]
    private bool hasLookupResult;

    /// <summary>true nếu tem này đã từng NG tại công đoạn của trạm — false thì các field lỗi bên dưới đều rỗng.</summary>
    [ObservableProperty]
    private bool hasNgHistory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LockStatusBrush))]
    private bool isTagLocked;

    [ObservableProperty]
    private string lockStatusLabel = string.Empty;

    [ObservableProperty]
    private string ngRejectionReason = string.Empty;

    [ObservableProperty]
    private string ngConfirmedByUserName = string.Empty;

    /// <summary>Thời điểm NG gần nhất, quy đổi giờ địa phương để hiển thị (API-Conventions.md mục 10 — lưu/truyền UTC, chỉ quy đổi ở tầng UI lúc render).</summary>
    [ObservableProperty]
    private string ngScannedAtLocalLabel = string.Empty;

    [ObservableProperty]
    private int ngCount;

    /// <summary>Màu nhãn trạng thái khóa — đỏ (StatusNgBrush) khi đang khóa, xanh (StatusOkBrush) khi đã mở khóa/chưa từng NG.</summary>
    public Brush LockStatusBrush => (Brush)(Application.Current?.TryFindResource(IsTagLocked ? "StatusNgBrush" : "StatusOkBrush") ?? Brushes.Gray);

    public ReworkUnlockViewModel(IReworkUnlockApiClient apiClient, StationOptions options, IServiceProvider serviceProvider)
    {
        _apiClient = apiClient;
        _options = options;
        _serviceProvider = serviceProvider;
        WorkStationName = options.WorkStationName;
        StageName = options.StageName;
    }

    /// <summary>Đổi mã tem -> thông tin lỗi đã tra cứu (nếu có) không còn khớp -> ẩn đi, tránh hiển thị nhầm lỗi của tem khác.</summary>
    partial void OnTagCodeChanged(string value)
    {
        if (HasLookupResult)
        {
            HasLookupResult = false;
        }
    }

    /// <summary>
    /// AC7: hiển thị popup đăng nhập Tổ trưởng (re-auth mỗi lần, tái sử dụng <see cref="Views.LoginDialog"/>/
    /// <see cref="LoginDialogViewModel"/> đã có ở US-18) nếu chưa có token đang giữ. Trả về true (VÀ đảm bảo
    /// <see cref="_supervisorAccessToken"/> có giá trị) nếu đã/đang có phiên hợp lệ; false nếu người dùng bấm Hủy
    /// hoặc dialog đóng mà không đăng nhập được (sai tài khoản/thiếu quyền dialog tự ở lại cho tới khi Hủy, cùng
    /// idiom <c>AndonBoardViewModel.AuthenticateForNgMode</c>).
    /// </summary>
    private bool EnsureAuthenticated()
    {
        if (!string.IsNullOrEmpty(_supervisorAccessToken))
        {
            return true;
        }

        var dialog = _serviceProvider.GetRequiredService<Views.LoginDialog>();
        dialog.ViewModel.RequiredPermission = ReworkUnlockPermission;
        dialog.Owner = Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault();

        var loggedIn = dialog.ShowDialog() == true;
        if (!loggedIn)
        {
            return false;
        }

        _supervisorAccessToken = dialog.ViewModel.NgConfirmationLoginResult?.AccessToken;
        return !string.IsNullOrEmpty(_supervisorAccessToken);
    }

    private bool CanUnlock() => !IsBusy;

    private bool CanLookup() => !IsLookingUp;

    /// <summary>Tra cứu thông tin lỗi NG gần nhất + trạng thái khóa hiện tại của <see cref="TagCode"/> (feedback 18/08/2026) — chỉ hiển thị tham khảo.</summary>
    [RelayCommand(CanExecute = nameof(CanLookup))]
    private async Task LookupAsync()
    {
        var trimmedTagCode = TagCode.Trim();
        HasLookupResult = false;
        LookupMessage = string.Empty;
        if (trimmedTagCode.Length == 0)
        {
            LookupMessage = "Vui lòng nhập/scan mã tem cần tra cứu.";
            return;
        }

        // AC7: chức năng này bắt buộc đăng nhập lại — tra cứu (endpoint cùng policy Scan.ReworkUnlock như mở
        // khóa) cũng cần token hợp lệ, tái dùng token đang giữ (nếu có) thay vì tự xóa sau mỗi lần tra cứu.
        if (!EnsureAuthenticated())
        {
            LookupMessage = "Cần đăng nhập Tổ trưởng (có quyền Mở khóa rework) để tra cứu.";
            return;
        }

        IsLookingUp = true;
        try
        {
            var status = await _apiClient.GetLockStatusAsync(trimmedTagCode, _options.WorkStationId, _supervisorAccessToken!);
            HasNgHistory = status.HasNgHistory;
            IsTagLocked = status.IsLocked;
            NgCount = status.NgCount;

            if (!status.HasNgHistory)
            {
                LockStatusLabel = "Tem chưa từng NG tại công đoạn này.";
                NgRejectionReason = string.Empty;
                NgConfirmedByUserName = string.Empty;
                NgScannedAtLocalLabel = string.Empty;
            }
            else
            {
                NgRejectionReason = status.RejectionReason ?? "(không có lý do)";
                NgConfirmedByUserName = string.IsNullOrWhiteSpace(status.NgConfirmedByUserName) ? "(không rõ)" : status.NgConfirmedByUserName;
                // Đổi ý 19/08/2026: NgScannedAtUtc nay đã là giờ local nhà máy, KHÔNG quy đổi nữa (xem API-Conventions.md mục 10).
                NgScannedAtLocalLabel = status.NgScannedAtUtc.HasValue
                    ? status.NgScannedAtUtc.Value.ToString("dd/MM/yyyy HH:mm:ss")
                    : string.Empty;
                LockStatusLabel = status.IsLocked ? "Đang bị khóa rework — chưa thể scan lại." : "Đã được mở khóa — có thể scan lại bình thường.";
            }

            HasLookupResult = true;
        }
        catch (ApiException ex)
        {
            LookupMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            LookupMessage = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            LookupMessage = NetworkErrorMessage.ForTimeout();
        }
        finally
        {
            IsLookingUp = false;
        }
    }

    /// <summary>AC2: xác nhận mở khóa rework cho <see cref="TagCode"/> tại công đoạn của trạm này.</summary>
    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private async Task UnlockAsync()
    {
        var trimmedTagCode = TagCode.Trim();
        if (trimmedTagCode.Length == 0)
        {
            StatusMessage = "Vui lòng nhập/scan mã tem cần mở khóa.";
            IsLastResultSuccess = false;
            return;
        }

        // AC7: bắt buộc đăng nhập lại (hoặc tái dùng token vừa đăng nhập cho lần tra cứu ngay trước đó trong
        // cùng lần vào màn hình này) TRƯỚC khi gọi API mở khóa.
        if (!EnsureAuthenticated())
        {
            StatusMessage = "Cần đăng nhập Tổ trưởng (có quyền Mở khóa rework) để xác nhận mở khóa.";
            IsLastResultSuccess = false;
            return;
        }

        // FR-19 "chỉ có hiệu lực 1 lần dùng": xóa token NGAY trước khi gửi request — muốn mở khóa tem tiếp theo
        // (dù cùng lần vào màn hình) bắt buộc đăng nhập lại, cùng idiom ConfirmNgReasonAsync (US-18).
        var accessToken = _supervisorAccessToken!;
        _supervisorAccessToken = null;

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _apiClient.UnlockAsync(trimmedTagCode, _options.WorkStationId, string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(), accessToken);
            StatusMessage = $"✓ Đã mở khóa rework cho tem \"{result.TagCode}\" — công nhân có thể scan lại tại công đoạn \"{StageName}\".";
            IsLastResultSuccess = true;
            TagCode = string.Empty;
            Note = string.Empty;
        }
        catch (ApiException ex)
        {
            StatusMessage = ex.Message;
            IsLastResultSuccess = false;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = NetworkErrorMessage.ForConnectionFailure(ex);
            IsLastResultSuccess = false;
        }
        catch (TaskCanceledException)
        {
            StatusMessage = NetworkErrorMessage.ForTimeout();
            IsLastResultSuccess = false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
