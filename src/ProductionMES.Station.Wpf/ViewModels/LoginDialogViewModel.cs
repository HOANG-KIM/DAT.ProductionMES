using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
            await _authService.LoginAsync(Username, password);
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
