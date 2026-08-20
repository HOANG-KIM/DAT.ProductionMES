using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Configuration;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.Lines;
using ProductionMES.Station.Wpf.Services.Stages;
using ProductionMES.Station.Wpf.Services.WorkStations;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Cấu hình trạm" — cho Tổ trưởng (đăng nhập nâng quyền) sửa 1 phần <see cref="StationOptions"/>
/// qua UI thay vì sửa tay <c>appsettings.json</c> tại trạm, giảm rủi ro lệch dữ liệu giữa Id và tên hiển thị.
/// KHÔNG đưa vào đây <c>ApiBaseUrl</c>/<c>SignalRHubUrl</c>/<c>StationApiKeyValue</c> — cấu hình kết nối/bí mật,
/// vẫn phải sửa tay JSON (đổi qua UI rủi ro cao hơn lợi ích).
/// </summary>
/// <remarks>
/// Chỉ cho chọn <see cref="SelectedWorkStation"/> từ danh mục (KHÔNG cho gõ tay riêng lẻ LineId/LineName/StageId/
/// StageName/WorkStationName) — chọn xong tự suy ra và ghi đè cả 4 giá trị đó từ đúng
/// <see cref="WorkStationDto.LineId"/>/<see cref="WorkStationDto.StageId"/> đối chiếu ngược lại danh sách
/// Line/Stage đã tải (<see cref="OnSelectedWorkStationChanged"/>) — đây chính là điểm mấu chốt loại bỏ rủi ro lệch
/// dữ liệu giữa Id và tên hiển thị mà JSON tay đang gặp.
/// </remarks>
/// <remarks>
/// <see cref="StationOptions"/> là singleton đã inject xuyên suốt app (timer/SignalR group ở
/// <c>AndonBoardViewModel</c> đã set sẵn giá trị cũ lúc khởi tạo) — KHÔNG mutate live instance đó sau khi lưu (dễ
/// tạo trạng thái nửa vời). <see cref="SaveAsync"/> chỉ ghi xuống file <c>appsettings.json</c>, rồi yêu cầu khởi
/// động lại ứng dụng để áp dụng (qua <see cref="Views.RestartPromptDialog"/>).
/// </remarks>
public partial class StationConfigViewModel : ObservableObject
{
    private readonly ILineApiClient _lineApiClient;
    private readonly IStageApiClient _stageApiClient;
    private readonly IWorkStationApiClient _workStationApiClient;
    private readonly StationOptions _options;

    [ObservableProperty]
    private ObservableCollection<LineDto> allLines = new();

    [ObservableProperty]
    private ObservableCollection<StageDto> allStages = new();

    /// <summary>Toàn bộ trạm làm việc (kể cả đã vô hiệu hóa) — nạp 1 lần lúc vào trang, nguồn duy nhất cho combobox chọn trạm.</summary>
    [ObservableProperty]
    private ObservableCollection<WorkStationDto> allWorkStations = new();

    /// <summary>Trạm đang chọn (thay cho gõ tay Id) — chọn xong tự suy ra <see cref="WorkStationName"/>/<see cref="LineId"/>/<see cref="LineName"/>/<see cref="StageId"/>/<see cref="StageName"/>.</summary>
    [ObservableProperty]
    private WorkStationDto? selectedWorkStation;

    /// <summary>Suy ra từ <see cref="SelectedWorkStation"/> — chỉ hiển thị readonly, không cho sửa tay riêng lẻ.</summary>
    [ObservableProperty]
    private string workStationName = string.Empty;

    private int lineId;

    /// <summary>Suy ra từ <see cref="SelectedWorkStation"/> — chỉ hiển thị readonly, không cho sửa tay riêng lẻ.</summary>
    [ObservableProperty]
    private string lineName = string.Empty;

    private int stageId;

    /// <summary>Suy ra từ <see cref="SelectedWorkStation"/> — chỉ hiển thị readonly, không cho sửa tay riêng lẻ.</summary>
    [ObservableProperty]
    private string stageName = string.Empty;

    [ObservableProperty]
    private int arduinoTimeoutSeconds;

    [ObservableProperty]
    private int ngModeTimeoutSeconds;

    [ObservableProperty]
    private int andonBoardRefreshIntervalSeconds;

    [ObservableProperty]
    private int supervisorSessionIdleTimeoutSeconds;

    [ObservableProperty]
    private bool enableManualScanInput;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// Báo cho <c>StationConfigPage</c> (code-behind) biết đã lưu file thành công, để hiện
    /// <see cref="Views.RestartPromptDialog"/> đúng Owner (Window chứa trang) — không tự mở dialog/khởi động lại
    /// ngay trong ViewModel (cần Window cụ thể để CenterOwner đúng, khác cách <c>PlanSettingsViewModel</c> dùng
    /// <see cref="System.Windows.MessageBox"/> owner-less vì không cần định vị chính xác).
    /// </summary>
    public event EventHandler? SavedSuccessfully;

    public StationConfigViewModel(
        ILineApiClient lineApiClient,
        IStageApiClient stageApiClient,
        IWorkStationApiClient workStationApiClient,
        StationOptions options)
    {
        _lineApiClient = lineApiClient;
        _stageApiClient = stageApiClient;
        _workStationApiClient = workStationApiClient;
        _options = options;

        // Prefill từ cấu hình hiện tại của trạm — SelectedWorkStation chỉ chọn được đúng sau khi LoadAsync tải
        // xong AllWorkStations (xem cách khớp lại theo Id ở LoadAsync).
        lineId = options.LineId;
        LineName = options.LineName;
        stageId = options.StageId;
        StageName = options.StageName;
        WorkStationName = options.WorkStationName;
        ArduinoTimeoutSeconds = options.ArduinoTimeoutSeconds;
        NgModeTimeoutSeconds = options.NgModeTimeoutSeconds;
        AndonBoardRefreshIntervalSeconds = options.AndonBoardRefreshIntervalSeconds;
        SupervisorSessionIdleTimeoutSeconds = options.SupervisorSessionIdleTimeoutSeconds;
        EnableManualScanInput = options.EnableManualScanInput;
    }

    /// <summary>Nạp song song Line/Stage/WorkStation lúc vào trang, rồi khớp lại <see cref="SelectedWorkStation"/> theo <see cref="StationOptions.WorkStationId"/> hiện tại.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var linesTask = _lineApiClient.GetAllAsync();
            var stagesTask = _stageApiClient.GetAllAsync();
            var workStationsTask = _workStationApiClient.GetAllAsync();
            await Task.WhenAll(linesTask, stagesTask, workStationsTask);

            AllLines = new ObservableCollection<LineDto>(linesTask.Result);
            AllStages = new ObservableCollection<StageDto>(stagesTask.Result);
            AllWorkStations = new ObservableCollection<WorkStationDto>(workStationsTask.Result);

            SelectedWorkStation = AllWorkStations.FirstOrDefault(w => w.Id == _options.WorkStationId);
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
    /// Chọn trạm xong tự suy ra và ghi đè WorkStationName + LineId/LineName + StageId/StageName từ đúng
    /// <see cref="WorkStationDto.LineId"/>/<see cref="WorkStationDto.StageId"/> đối chiếu ngược lại
    /// <see cref="AllLines"/>/<see cref="AllStages"/> đã tải — loại bỏ rủi ro lệch dữ liệu giữa Id và tên hiển thị.
    /// </summary>
    partial void OnSelectedWorkStationChanged(WorkStationDto? value)
    {
        if (value is null)
        {
            return;
        }

        WorkStationName = value.Name;
        lineId = value.LineId;
        LineName = AllLines.FirstOrDefault(l => l.Id == value.LineId)?.Name ?? $"#{value.LineId}";
        stageId = value.StageId;
        StageName = AllStages.FirstOrDefault(s => s.Id == value.StageId)?.Name ?? $"#{value.StageId}";
    }

    /// <summary>
    /// Validate rồi ghi đè đúng các key đã sửa xuống <c>appsettings.json</c> tại trạm (giữ nguyên mọi key khác,
    /// đặc biệt <c>StationApiKeyValue</c>) — KHÔNG deserialize/serialize lại toàn bộ object qua
    /// <see cref="StationOptions"/> (sẽ mất field không có trong ViewModel). Không mutate live
    /// <see cref="StationOptions"/> — chỉ hiện dialog yêu cầu khởi động lại sau khi lưu thành công.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = string.Empty;

        if (SelectedWorkStation is null)
        {
            StatusMessage = "Vui lòng chọn Trạm làm việc.";
            return;
        }

        if (ArduinoTimeoutSeconds <= 0)
        {
            StatusMessage = "Thời gian chờ Arduino (giây) phải lớn hơn 0.";
            return;
        }

        if (NgModeTimeoutSeconds <= 0)
        {
            StatusMessage = "Thời gian chờ chế độ Scan NG (giây) phải lớn hơn 0.";
            return;
        }

        if (AndonBoardRefreshIntervalSeconds <= 0)
        {
            StatusMessage = "Chu kỳ làm mới Andon Board (giây) phải lớn hơn 0.";
            return;
        }

        if (SupervisorSessionIdleTimeoutSeconds <= 0)
        {
            StatusMessage = "Thời gian timeout không hoạt động của phiên Tổ trưởng (giây) phải lớn hơn 0.";
            return;
        }

        IsBusy = true;
        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var json = await File.ReadAllTextAsync(filePath);
            var node = JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidOperationException("Không đọc được appsettings.json hiện tại.");

            node["WorkStationId"] = SelectedWorkStation.Id;
            node["WorkStationName"] = WorkStationName;
            node["LineId"] = lineId;
            node["LineName"] = LineName;
            node["StageId"] = stageId;
            node["StageName"] = StageName;
            node["ArduinoTimeoutSeconds"] = ArduinoTimeoutSeconds;
            node["NgModeTimeoutSeconds"] = NgModeTimeoutSeconds;
            node["AndonBoardRefreshIntervalSeconds"] = AndonBoardRefreshIntervalSeconds;
            node["SupervisorSessionIdleTimeoutSeconds"] = SupervisorSessionIdleTimeoutSeconds;
            node["EnableManualScanInput"] = EnableManualScanInput;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(filePath, node.ToJsonString(options));

            StatusMessage = "✓ Đã lưu cấu hình trạm.";
            SavedSuccessfully?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or JsonException)
        {
            StatusMessage = $"Không lưu được cấu hình: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
