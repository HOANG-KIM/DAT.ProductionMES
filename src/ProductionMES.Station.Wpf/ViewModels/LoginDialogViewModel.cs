using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Auth;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>ViewModel cho dialog đăng nhập Supervisor tại trạm (ADR-005) — mở khi bấm "Cài đặt kế hoạch"/"Chọn kế hoạch" mà chưa đăng nhập.</summary>
public partial class LoginDialogViewModel : ObservableObject
{
    private readonly ISupervisorAuthService _authService;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Báo cho code-behind biết đóng dialog và kết quả (true = đăng nhập thành công).</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>
    /// US-18 (thay đổi 18/08/2026) — khi != null: dialog chuyển sang luồng re-auth RIÊNG cho Scan NG
    /// (<see cref="ISupervisorAuthService.LoginForNgConfirmationAsync"/>, KHÔNG ghi vào session dùng chung
    /// <see cref="ISupervisorSessionService"/>), VÀ bắt buộc tài khoản đăng nhập phải có đúng permission này
    /// (dạng "Resource.Action", vd "Scan.ConfirmNg") — nếu không, hiển thị lỗi NGAY TẠI POPUP (AC2b), KHÔNG đóng
    /// dialog, cho phép thử lại/hủy. Mặc định null: giữ nguyên hành vi cũ (HomePage.RequireAuth, US-05/05a/05b).
    /// </summary>
    public string? RequiredPermission { get; set; }

    /// <summary>
    /// US-18: kết quả đăng nhập thành công gần nhất khi <see cref="RequiredPermission"/> được cấu hình — caller
    /// (<c>AndonBoardViewModel</c>) đọc <see cref="StationLoginResponse.AccessToken"/> từ đây sau khi
    /// <c>ShowDialog()</c> trả về true. Null nếu chưa đăng nhập theo luồng này.
    /// </summary>
    public StationLoginResponse? NgConfirmationLoginResult { get; private set; }

    public LoginDialogViewModel(ISupervisorAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            if (RequiredPermission is not null)
            {
                // US-18 AC2a/AC2b: re-auth riêng, KHÔNG set session dùng chung; đăng nhập đúng nhưng thiếu quyền
                // -> báo lỗi tại popup, KHÔNG đóng dialog (AC2b "cho phép thử đăng nhập lại hoặc hủy").
                var loginResult = await _authService.LoginForNgConfirmationAsync(Username, password);
                if (!loginResult.Permissions.Contains(RequiredPermission))
                {
                    ErrorMessage = "Tài khoản không có quyền xác nhận Scan NG.";
                    return;
                }

                NgConfirmationLoginResult = loginResult;
            }
            else
            {
                await _authService.LoginAsync(Username, password);
            }

            RequestClose?.Invoke(this, true);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            // Không kết nối được API (sai địa chỉ/port, API chưa chạy, lỗi chứng chỉ HTTPS...) — trước đây lỗi
            // loại này rơi ra ngoài mọi catch (chỉ bắt ApiException) nên dialog trông như "không phản hồi gì".
            ErrorMessage = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = NetworkErrorMessage.ForTimeout();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);
}
