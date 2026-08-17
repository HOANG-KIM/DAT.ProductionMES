using System.Windows;
using System.Windows.Input;
using ProductionMES.Station.Wpf.ViewModels;

namespace ProductionMES.Station.Wpf.Views;

/// <summary>
/// Dialog đăng nhập Supervisor — <see cref="PasswordBox"/> không bind trực tiếp qua MVVM (WPF không cho bind
/// <c>Password</c> vì lý do bảo mật), nên đọc giá trị ở code-behind lúc bấm Đăng nhập rồi truyền vào ViewModel.
/// </summary>
public partial class LoginDialog : Window
{
    private readonly LoginDialogViewModel _viewModel;

    public LoginDialog(LoginDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += OnRequestClose;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void OnRequestClose(object? sender, bool success)
    {
        DialogResult = success;
        Close();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await SubmitLoginAsync();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelCommand.Execute(null);
    }

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Chặn bubble tiếp lên Window — nếu không, nút "Đăng nhập" (IsDefault="True") sẽ tự bắt Enter lần
            // nữa và gọi trùng request.
            e.Handled = true;
            await SubmitLoginAsync();
        }
    }

    /// <summary>
    /// Vô hiệu hoá 2 nút trong lúc gọi API — trước đó không có dấu hiệu "đang xử lý" nào nên khi mạng chậm/API
    /// không phản hồi, dialog trông như bị đứng, đồng thời chặn bấm 2 lần (Enter + Click) gọi trùng request.
    /// </summary>
    private async Task SubmitLoginAsync()
    {
        if (_viewModel.LoginCommand.IsRunning)
        {
            return;
        }

        LoginButton.IsEnabled = false;
        CancelButtonElement.IsEnabled = false;
        var originalContent = LoginButton.Content;
        LoginButton.Content = "Đang đăng nhập...";
        try
        {
            await _viewModel.LoginCommand.ExecuteAsync(PasswordBox.Password);
        }
        finally
        {
            LoginButton.IsEnabled = true;
            CancelButtonElement.IsEnabled = true;
            LoginButton.Content = originalContent;
        }
    }
}
