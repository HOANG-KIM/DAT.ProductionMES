using CommunityToolkit.Mvvm.ComponentModel;
using ProductionMES.Station.Wpf.Configuration;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>ViewModel cho Trang chủ (Main Screen) — chỉ hiển thị thông tin trạm, điều hướng xử lý ở code-behind (HomePage).</summary>
public partial class HomeViewModel : ObservableObject
{
    public string WorkStationName { get; }

    public string LineName { get; }

    public string StageName { get; }

    public HomeViewModel(StationOptions options)
    {
        WorkStationName = options.WorkStationName;
        LineName = options.LineName;
        StageName = options.StageName;
    }
}
