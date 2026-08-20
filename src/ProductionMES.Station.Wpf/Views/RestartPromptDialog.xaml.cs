using System.Windows;

namespace ProductionMES.Station.Wpf.Views;

/// <summary>
/// Dialog thông báo sau khi lưu "Cấu hình trạm" thành công — <see cref="StationOptions"/> là singleton đã inject
/// xuyên suốt app, không thể mutate an toàn lúc đang chạy (timer/SignalR group đã set theo giá trị cũ), nên bắt
/// buộc khởi động lại để áp dụng cấu hình mới. 2 lựa chọn: khởi động lại ngay, hoặc để sau (đóng dialog, ở lại trang).
/// </summary>
public partial class RestartPromptDialog : Window
{
    /// <summary>True nếu người dùng chọn "Khởi động lại ngay".</summary>
    public bool RestartNow { get; private set; }

    public RestartPromptDialog()
    {
        InitializeComponent();
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        RestartNow = true;
        DialogResult = true;
        Close();
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        RestartNow = false;
        DialogResult = false;
        Close();
    }
}
