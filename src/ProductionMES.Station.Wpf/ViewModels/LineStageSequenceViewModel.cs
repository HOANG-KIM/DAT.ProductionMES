using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.LineStageSequences;
using ProductionMES.Station.Wpf.Services.Lines;
using ProductionMES.Station.Wpf.Services.Stages;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Trình tự công đoạn của Line" (US-03) — thay thế khu vực "Công đoạn của kế hoạch" đã bỏ khỏi
/// <c>PlanSettingsPage</c>/<c>PlanSettingsViewModel</c> (17/08/2026): trình tự công đoạn không còn là cấu hình
/// riêng theo từng kế hoạch mà là cấu hình của Line, thiết lập 1 lần, dùng chung cho mọi kế hoạch chạy trên Line
/// đó. Line đang cấu hình lấy mặc định từ <see cref="StationOptions.LineId"/> (cấu hình cục bộ tại trạm — xem
/// cách <c>PlanSettingsViewModel</c> cũ lấy LineId làm mẫu).
/// </summary>
/// <remarks>
/// Sửa lại 17/08/2026 (US-03 AC8): bỏ ô nhập tay <c>LineId</c> trên UI — Line của màn này luôn cố định theo trạm
/// (khác US-05b, nơi Tổ trưởng được chọn công đoạn khác từ xa), chỉ hiển thị <see cref="LineName"/> readonly, tra
/// theo <see cref="StationOptions.LineId"/> qua <see cref="ILineApiClient"/> (US-01).
/// </remarks>
public partial class LineStageSequenceViewModel : ObservableObject
{
    private readonly ILineStageSequenceApiClient _apiClient;
    private readonly IStageApiClient _stageCatalogApiClient;
    private readonly ILineApiClient _lineApiClient;

    [ObservableProperty]
    private int lineId;

    /// <summary>Tên Line cố định của trạm (AC8) — hiển thị readonly, không cho đổi từ màn này.</summary>
    [ObservableProperty]
    private string lineName = string.Empty;

    /// <summary>Trình tự công đoạn hiện tại của Line, sắp theo SequenceNumber — dùng <see cref="LineStageSequenceRow"/>
    /// (wrapper cục bộ) để hiển thị kèm <c>StageName</c> đã join với <see cref="AvailableStages"/>.</summary>
    [ObservableProperty]
    private ObservableCollection<LineStageSequenceRow> sequence = new();

    /// <summary>Danh mục Công đoạn master (US-02) để chọn theo tên khi gắn vào trình tự — nạp 1 lần lúc vào trang.</summary>
    [ObservableProperty]
    private ObservableCollection<StageDto> availableStages = new();

    /// <summary>Công đoạn đang chọn trong combobox để gắn vào trình tự (AC1) — chọn theo tên, không nhập tay Id.</summary>
    [ObservableProperty]
    private StageDto? selectedNewStage;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public LineStageSequenceViewModel(
        ILineStageSequenceApiClient apiClient,
        IStageApiClient stageCatalogApiClient,
        ILineApiClient lineApiClient,
        StationOptions options)
    {
        _apiClient = apiClient;
        _stageCatalogApiClient = stageCatalogApiClient;
        _lineApiClient = lineApiClient;
        LineId = options.LineId;
    }

    /// <summary>Nạp tên Line cố định của trạm (AC8) — gọi 1 lần lúc vào trang, chỉ để hiển thị readonly.</summary>
    [RelayCommand]
    private async Task LoadLineInfoAsync()
    {
        try
        {
            var items = await _lineApiClient.GetAllAsync();
            LineName = items.FirstOrDefault(l => l.Id == LineId)?.Name ?? $"#{LineId}";
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

    /// <summary>Nạp danh mục Công đoạn (US-02) cho combobox gắn công đoạn — gọi 1 lần lúc vào trang.</summary>
    [RelayCommand]
    private async Task LoadStagesAsync()
    {
        try
        {
            var items = await _stageCatalogApiClient.GetAllAsync();
            AvailableStages = new ObservableCollection<StageDto>(items.Where(s => s.IsActive));
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

    /// <summary>Tải lại trình tự công đoạn hiện tại của Line.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var items = await _apiClient.GetByLineAsync(LineId);
            Sequence = new ObservableCollection<LineStageSequenceRow>(MapToRows(items));
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

    /// <summary>Gắn công đoạn đang chọn ở <see cref="SelectedNewStage"/> vào trình tự của Line (AC1).</summary>
    [RelayCommand]
    private async Task AddStageAsync()
    {
        if (SelectedNewStage is null)
        {
            return;
        }

        var stageName = SelectedNewStage.Name;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.AddStageAsync(LineId, SelectedNewStage.Id, sequenceNumber: null);
            SelectedNewStage = null;
            StatusMessage = $"✓ Đã gắn công đoạn \"{stageName}\" vào trình tự của Line.";
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

    /// <summary>
    /// Gỡ công đoạn <paramref name="stageId"/> khỏi trình tự của Line (AC2) — server từ chối (409) nếu đang có
    /// kế hoạch Running/Paused tại đúng công đoạn này, hiển thị nguyên văn lý do server trả về.
    /// </summary>
    [RelayCommand]
    private async Task RemoveStageAsync(int stageId)
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.RemoveStageAsync(LineId, stageId);
            var item = Sequence.FirstOrDefault(s => s.StageId == stageId);
            if (item is not null)
            {
                Sequence.Remove(item);
            }

            var stageName = AvailableStages.FirstOrDefault(s => s.Id == stageId)?.Name ?? $"#{stageId}";
            StatusMessage = $"✓ Đã gỡ công đoạn \"{stageName}\" khỏi trình tự của Line.";
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

    /// <summary>Đổi công đoạn <paramref name="stageId"/> lên trước 1 vị trí trong trình tự (AC3).</summary>
    [RelayCommand]
    private Task MoveStageUpAsync(int stageId) => MoveStageAsync(stageId, offset: -1);

    /// <summary>Đổi công đoạn <paramref name="stageId"/> xuống sau 1 vị trí trong trình tự (AC3).</summary>
    [RelayCommand]
    private Task MoveStageDownAsync(int stageId) => MoveStageAsync(stageId, offset: 1);

    /// <summary>
    /// Đổi chỗ công đoạn <paramref name="stageId"/> với công đoạn liền kề (theo <paramref name="offset"/> = -1 lên/+1 xuống)
    /// trong danh sách hiện tại, rồi gửi lại toàn bộ trình tự mới (đánh số liên tục 1..n) cho server (AC3).
    /// AC4 (trùng số thứ tự)/AC5 (vòng lặp) đã được server đảm bảo — client chỉ hiển thị lỗi nếu server từ chối.
    /// </summary>
    private async Task MoveStageAsync(int stageId, int offset)
    {
        var ordered = Sequence.OrderBy(x => x.SequenceNumber).ToList();
        var index = ordered.FindIndex(x => x.StageId == stageId);
        var targetIndex = index + offset;
        if (index < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
        {
            return;
        }

        (ordered[index], ordered[targetIndex]) = (ordered[targetIndex], ordered[index]);

        var items = ordered
            .Select((row, i) => (row.StageId, SequenceNumber: i + 1))
            .ToList();

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _apiClient.ReorderAsync(LineId, items);
            Sequence = new ObservableCollection<LineStageSequenceRow>(MapToRows(result));
            StatusMessage = "✓ Đã cập nhật trình tự công đoạn của Line.";
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

    /// <summary>Join <see cref="LineStageSequenceDto"/> (server) với <see cref="AvailableStages"/> để có tên hiển thị.</summary>
    private List<LineStageSequenceRow> MapToRows(IEnumerable<LineStageSequenceDto> items)
    {
        return items
            .OrderBy(x => x.SequenceNumber)
            .Select(x => new LineStageSequenceRow(
                x.Id,
                x.StageId,
                AvailableStages.FirstOrDefault(s => s.Id == x.StageId)?.Name ?? $"#{x.StageId}",
                x.SequenceNumber))
            .ToList();
    }
}

/// <summary>
/// Wrapper hiển thị cho DataGrid "Trình tự công đoạn của Line" (US-03) — thay vì bind thẳng
/// <see cref="LineStageSequenceDto"/> (chỉ có <c>StageId</c> thô), gộp thêm <see cref="StageName"/> đã join với
/// danh mục Công đoạn (<c>AvailableStages</c>).
/// </summary>
public sealed record LineStageSequenceRow(int Id, int StageId, string StageName, int SequenceNumber);
