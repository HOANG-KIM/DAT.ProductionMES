using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.Lines;
using ProductionMES.Station.Wpf.Services.ProductionPlanStages;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Chọn kế hoạch" (US-05b). Combobox "Công đoạn" hiện chỉ liệt kê đúng 1 công đoạn cấu hình cục
/// bộ cho trạm (<see cref="StationOptions.StageId"/>) — CHƯA đủ AC1 (liệt kê MỌI công đoạn của Line, không giới
/// hạn theo trạm vật lý) vì chưa có API tra cứu danh sách công đoạn theo Line ở bản này; ghi rõ để làm tiếp.
/// </summary>
/// <remarks>
/// Sửa lại 17/08/2026: bỏ dòng chữ hardcode cứng "Line 1 (theo trạm đang đăng nhập)" — <see cref="LineName"/>
/// tra tên Line thật của trạm (<see cref="StationOptions.LineId"/>) qua <see cref="ILineApiClient"/> (US-01),
/// nạp 1 lần lúc vào trang.
/// </remarks>
public partial class PlanSelectionViewModel : ObservableObject
{
    private readonly IProductionPlanStageApiClient _apiClient;
    private readonly ILineApiClient _lineApiClient;
    private readonly int _lineId;

    [ObservableProperty]
    private ObservableCollection<ProductionPlanStageSelectionDto> plans = new();

    [ObservableProperty]
    private ProductionPlanStageSelectionDto? selectedPlan;

    [ObservableProperty]
    private bool includeClosed;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Tên Line thật của trạm — thay cho chữ hardcode "Line 1" trước đây.</summary>
    [ObservableProperty]
    private string lineName = string.Empty;

    public string StageName { get; }

    public int StageId { get; }

    public PlanSelectionViewModel(IProductionPlanStageApiClient apiClient, ILineApiClient lineApiClient, StationOptions options)
    {
        _apiClient = apiClient;
        _lineApiClient = lineApiClient;
        _lineId = options.LineId;
        StageId = options.StageId;
        StageName = options.StageName;
    }

    /// <summary>Nạp tên Line thật của trạm (bug hardcode "Line 1") — gọi 1 lần lúc vào trang.</summary>
    [RelayCommand]
    private async Task LoadLineInfoAsync()
    {
        try
        {
            var items = await _lineApiClient.GetAllAsync();
            LineName = items.FirstOrDefault(l => l.Id == _lineId)?.Name ?? $"#{_lineId}";
        }
        catch (ApiException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            StatusMessage = NetworkErrorMessage.ForTimeout();
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        SelectedPlan = null;
        try
        {
            var items = await _apiClient.GetByLineAndStageAsync(_lineId, StageId, IncludeClosed);
            Plans = new ObservableCollection<ProductionPlanStageSelectionDto>(items);
        }
        catch (ApiException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            StatusMessage = NetworkErrorMessage.ForTimeout();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.ApplyAsync(SelectedPlan.ProductionPlanId, StageId);
            StatusMessage = $"✓ Đã áp dụng {SelectedPlan.Lot} cho {StageName}.";
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            // US-05a AC1: server từ chối nếu (Line, Công đoạn) đang có kế hoạch KHÁC Running — không có đường
            // ghi đè, chỉ có thể Tạm dừng/Đóng kế hoạch đang chạy trước rồi thử lại (đúng như message server trả về).
            MessageBox.Show(ex.Message, "Không thể áp dụng", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            StatusMessage = NetworkErrorMessage.ForTimeout();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PauseAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.PauseAsync(SelectedPlan.ProductionPlanId, StageId);
            StatusMessage = $"✓ Đã tạm dừng {SelectedPlan.Lot}.";
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            StatusMessage = NetworkErrorMessage.ForTimeout();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        await CloseWithConfirmRetryAsync(SelectedPlan.ProductionPlanId, confirm: false);
    }

    private async Task CloseWithConfirmRetryAsync(int productionPlanId, bool confirm)
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.CloseAsync(productionPlanId, StageId, confirm);
            StatusMessage = "✓ Đã đóng kế hoạch.";
            await LoadAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict && !confirm)
        {
            var proceed = MessageBox.Show(
                ex.Message + "\n\nXác nhận đóng sớm?",
                "Cần xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;

            if (proceed)
            {
                await CloseWithConfirmRetryAsync(productionPlanId, confirm: true);
            }
        }
        catch (ApiException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = NetworkErrorMessage.ForConnectionFailure(ex);
        }
        catch (TaskCanceledException)
        {
            StatusMessage = NetworkErrorMessage.ForTimeout();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnIncludeClosedChanged(bool value) => _ = LoadAsync();
}
