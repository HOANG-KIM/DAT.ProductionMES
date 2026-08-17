using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.LineStageSequences;
using ProductionMES.Station.Wpf.Services.Lines;
using ProductionMES.Station.Wpf.Services.ProductionPlanStages;
using ProductionMES.Station.Wpf.Services.Stages;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Chọn kế hoạch" (US-05b). Combobox "Công đoạn" liệt kê MỌI công đoạn đã cấu hình trong trình tự
/// của Line (<see cref="ILineStageSequenceApiClient.GetByLineAsync"/>, US-03) — không giới hạn theo công đoạn vật
/// lý của trạm đang đứng, cho phép Tổ trưởng cấu hình kế hoạch cho công đoạn khác từ xa (AC1).
/// </summary>
/// <remarks>
/// Sửa lại 17/08/2026: bỏ dòng chữ hardcode cứng "Line 1 (theo trạm đang đăng nhập)" — <see cref="LineName"/>
/// tra tên Line thật của trạm (<see cref="StationOptions.LineId"/>) qua <see cref="ILineApiClient"/> (US-01),
/// nạp 1 lần lúc vào trang.
/// </remarks>
/// <remarks>
/// Sửa lại 17/08/2026 (đóng gap AC1): <c>StageId</c>/<c>StageName</c> (get-only, cố định theo trạm) đổi thành
/// <see cref="AvailableStages"/>/<see cref="SelectedStage"/> — nạp bằng cách join <see cref="LineStageSequenceDto"/>
/// (trình tự của Line, US-03) với danh mục Công đoạn (<see cref="IStageApiClient"/>, US-02) để lấy tên, giống hệt
/// cách <c>LineStageSequenceViewModel.MapToRows</c> đã làm. Mặc định chọn công đoạn vật lý của trạm
/// (<see cref="StationOptions.StageId"/>) nếu công đoạn đó có trong trình tự của Line, để giữ hành vi cũ.
/// </remarks>
public partial class PlanSelectionViewModel : ObservableObject
{
    private readonly IProductionPlanStageApiClient _apiClient;
    private readonly ILineApiClient _lineApiClient;
    private readonly ILineStageSequenceApiClient _lineStageSequenceApiClient;
    private readonly IStageApiClient _stageApiClient;
    private readonly int _lineId;
    private readonly int _defaultStageId;

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

    /// <summary>Mọi công đoạn đã cấu hình trong trình tự của Line này (US-03), sắp theo SequenceNumber — nạp 1
    /// lần lúc vào trang (AC1: KHÔNG giới hạn theo công đoạn vật lý của trạm).</summary>
    [ObservableProperty]
    private ObservableCollection<StageDto> availableStages = new();

    /// <summary>Công đoạn đang chọn để xem/áp dụng kế hoạch — mặc định là công đoạn vật lý của trạm nếu có trong
    /// trình tự của Line, cho phép đổi sang công đoạn khác (AC1: cấu hình từ xa).</summary>
    [ObservableProperty]
    private StageDto? selectedStage;

    public PlanSelectionViewModel(
        IProductionPlanStageApiClient apiClient,
        ILineApiClient lineApiClient,
        ILineStageSequenceApiClient lineStageSequenceApiClient,
        IStageApiClient stageApiClient,
        StationOptions options)
    {
        _apiClient = apiClient;
        _lineApiClient = lineApiClient;
        _lineStageSequenceApiClient = lineStageSequenceApiClient;
        _stageApiClient = stageApiClient;
        _lineId = options.LineId;
        _defaultStageId = options.StageId;
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

    /// <summary>Nạp danh sách công đoạn cho combobox (AC1) — join trình tự của Line (US-03) với danh mục Công
    /// đoạn (US-02) để lấy tên, giống hệt cách <c>LineStageSequenceViewModel.MapToRows</c> làm. Mặc định chọn
    /// công đoạn vật lý của trạm nếu có trong trình tự, nếu không thì chọn phần tử đầu tiên.</summary>
    [RelayCommand]
    private async Task LoadStagesAsync()
    {
        try
        {
            var sequence = await _lineStageSequenceApiClient.GetByLineAsync(_lineId);
            var catalog = await _stageApiClient.GetAllAsync();

            var stages = sequence
                .OrderBy(x => x.SequenceNumber)
                .Select(x => catalog.FirstOrDefault(s => s.Id == x.StageId)
                    ?? new StageDto { Id = x.StageId, Name = $"#{x.StageId}", IsActive = false })
                .ToList();

            AvailableStages = new ObservableCollection<StageDto>(stages);
            SelectedStage = AvailableStages.FirstOrDefault(s => s.Id == _defaultStageId) ?? AvailableStages.FirstOrDefault();
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
        if (SelectedStage is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        SelectedPlan = null;
        try
        {
            var items = await _apiClient.GetByLineAndStageAsync(_lineId, SelectedStage.Id, IncludeClosed);
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
        if (SelectedPlan is null || SelectedStage is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.ApplyAsync(SelectedPlan.ProductionPlanId, SelectedStage.Id);
            StatusMessage = $"✓ Đã áp dụng {SelectedPlan.Lot} cho {SelectedStage.Name}.";
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
        if (SelectedPlan is null || SelectedStage is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.PauseAsync(SelectedPlan.ProductionPlanId, SelectedStage.Id);
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
        if (SelectedStage is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.CloseAsync(productionPlanId, SelectedStage.Id, confirm);
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

    /// <summary>Đổi công đoạn (AC1) thì tự tải lại danh sách kế hoạch của (Line, công đoạn mới) — gọi sau khi
    /// <see cref="LoadStagesAsync"/> gán giá trị mặc định lần đầu, và mỗi khi người dùng chọn lại trên combobox.</summary>
    partial void OnSelectedStageChanged(StageDto? value) => _ = LoadAsync();
}
