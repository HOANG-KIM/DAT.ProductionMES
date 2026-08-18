using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.ReworkUnlocks;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Mở khóa rework" (US-19 AC2/AC6) — Tổ trưởng nhập/scan mã tem đang bị khóa do NG, xem lại tại
/// đúng công đoạn của trạm này (cố định theo <see cref="StationOptions"/>, giống <c>LineStageSequenceViewModel</c>
/// US-03 — Tổ trưởng KHÔNG chọn công đoạn khác từ màn này), nhập ghi chú tùy chọn rồi xác nhận mở khóa.
/// </summary>
/// <remarks>
/// Quyền hạn (AC6) thực thi ở server (<c>[Authorize(Policy = Scan.ReworkUnlock)]</c>) — nếu tài khoản đang đăng
/// nhập (qua <c>HomePage.RequireAuth</c>, phiên dùng chung <c>ISupervisorSessionService</c>) không có quyền này,
/// <see cref="UnlockAsync"/> nhận 403 từ server và hiển thị nguyên văn lỗi qua <see cref="StatusMessage"/> — không
/// tự chặn ở UI (đơn giản, nhất quán với cách <c>PlanSettingsPage</c>/<c>LineStageSequencePage</c> đang làm).
/// </remarks>
public partial class ReworkUnlockViewModel : ObservableObject
{
    private readonly IReworkUnlockApiClient _apiClient;
    private readonly StationOptions _options;

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

    public ReworkUnlockViewModel(IReworkUnlockApiClient apiClient, StationOptions options)
    {
        _apiClient = apiClient;
        _options = options;
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

        IsLookingUp = true;
        try
        {
            var status = await _apiClient.GetLockStatusAsync(trimmedTagCode, _options.WorkStationId);
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
                NgScannedAtLocalLabel = status.NgScannedAtUtc.HasValue
                    ? status.NgScannedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
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

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _apiClient.UnlockAsync(trimmedTagCode, _options.WorkStationId, string.IsNullOrWhiteSpace(Note) ? null : Note.Trim());
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
