using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// 1 dòng trong bảng PLAN/ACTUAL/BALANCE theo mốc giờ (US-09 AC1/AC5/AC6) hiển thị trên <c>AndonBoardWindow</c>.
/// Là <c>ObservableObject</c> (không phải record/DTO thuần) vì <see cref="ActualCumulative"/>/<see cref="Balance"/>
/// của dòng "hiện tại" (<see cref="IsCurrent"/> = true) cần cập nhật tại chỗ (in-place) mỗi khi có sự kiện
/// SignalR <c>ScanRecorded</c> mới (AC4, xem <c>AndonBoardViewModel.OnScanRecorded</c>) mà không cần tải lại
/// toàn bộ bảng từ server.
/// </summary>
public partial class AndonBoardRowViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeLabel))]
    private DateTime timeMarkLocal;

    [ObservableProperty]
    private int planCumulative;

    [ObservableProperty]
    private int actualCumulative;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BalanceLabel))]
    [NotifyPropertyChangedFor(nameof(BalanceBrush))]
    private int balance;

    [ObservableProperty]
    private bool isCurrent;

    public string TimeLabel => TimeMarkLocal.ToString("HH:mm");

    /// <summary>AC2/AC3: hiển thị dấu "+" rõ ràng cho giá trị dương (âm đã có sẵn dấu "-" từ ToString mặc định).</summary>
    public string BalanceLabel => Balance > 0 ? $"+{Balance}" : Balance.ToString();

    /// <summary>AC2/AC3: dương → xanh, âm → đỏ; bằng 0 → trung tính (không thuộc AC nào, chọn màu trắng cho rõ ràng).</summary>
    public Brush BalanceBrush => AndonBoardColors.GetBalanceBrush(Balance);
}
