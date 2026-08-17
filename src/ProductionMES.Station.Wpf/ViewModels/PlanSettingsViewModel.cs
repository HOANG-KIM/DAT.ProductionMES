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
using ProductionMES.Station.Wpf.Services.ProductionPlans;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Cài đặt kế hoạch sản xuất" (US-05). Không tự khoá trước các trường Khách hàng/Model/Lot/
/// Revision khi kế hoạch đã có scan (AC6) — vì <see cref="ProductionPlanDto"/> không mang cờ đó — mà dựa vào
/// chính lỗi 409 server trả về (kèm giải thích rõ) để báo cho Tổ trưởng, đúng nguồn sự thật duy nhất là
/// <c>ProductionPlanService.UpdateAsync</c>. Tương tự AC5 (xác nhận sửa Số lượng/Takt time khi đang chạy dở):
/// lưu thử với <c>Confirm=false</c> trước, nếu server từ chối (409) thì hỏi lại rồi gửi lại với <c>Confirm=true</c>.
/// </summary>
/// <remarks>
/// Sửa lại 17/08/2026 (US-03): đã bỏ hẳn khu vực "Công đoạn của kế hoạch" — trình tự công đoạn không còn là cấu
/// hình riêng theo từng kế hoạch mà là cấu hình của Line, thiết lập 1 lần, dùng chung cho mọi kế hoạch chạy trên
/// Line đó. Xem màn hình mới <c>LineStageSequencePage</c>/<c>LineStageSequenceViewModel</c>.
/// </remarks>
/// <remarks>
/// Sửa lại 17/08/2026 (US-05 AC1a/AC1b/AC1c): ô "Line áp dụng" đổi từ gõ tay số Id sang chọn theo tên qua
/// <see cref="SelectedLine"/> (combobox), theo đúng cách <c>LineStageSequenceViewModel</c> đã làm với Công đoạn.
/// <see cref="LineId"/> vẫn giữ lại làm giá trị nội bộ gửi API (không đổi payload) — được đồng bộ tự động mỗi khi
/// <see cref="SelectedLine"/> đổi. Không cho đổi Line khi đang sửa kế hoạch cũ (<c>UpdateProductionPlanRequest</c>
/// không có trường LineId — server không hỗ trợ đổi Line sau khi tạo), combobox bị khoá (<see cref="CanEditLine"/>)
/// trong trường hợp này nhưng vẫn hiển thị đúng tên Line hiện tại.
/// </remarks>
public partial class PlanSettingsViewModel : ObservableObject
{
    private readonly IProductionPlanApiClient _apiClient;
    private readonly ILineApiClient _lineApiClient;

    [ObservableProperty]
    private ObservableCollection<ProductionPlanDto> plans = new();

    [ObservableProperty]
    private ProductionPlanDto? selectedPlan;

    [ObservableProperty]
    private string formTitle = "Tạo kế hoạch mới";

    [ObservableProperty]
    private int? editingId;

    /// <summary>Giá trị Line thật sự gửi lên API — đồng bộ tự động theo <see cref="SelectedLine"/>, không còn bind trực tiếp lên UI (AC1a).</summary>
    [ObservableProperty]
    private int lineId;

    /// <summary>Toàn bộ danh mục Line (kể cả đã vô hiệu hóa) — nạp 1 lần lúc vào trang, dùng làm nguồn tra cứu tên cho AC1b/AC1c.</summary>
    [ObservableProperty]
    private ObservableCollection<LineDto> allLines = new();

    /// <summary>Danh sách hiển thị trong combobox chọn Line (AC1a) — chỉ gồm Line đang hoạt động, cộng thêm Line
    /// đang gán cho <see cref="SelectedPlan"/> hiện tại dù đã vô hiệu hóa (AC1b), tính lại mỗi khi <see cref="AllLines"/>
    /// hoặc <see cref="SelectedPlan"/> đổi.</summary>
    [ObservableProperty]
    private ObservableCollection<LineDto> availableLines = new();

    /// <summary>Line đang chọn trong combobox (AC1a) — thay cho việc gõ tay <see cref="LineId"/> trước đây.</summary>
    [ObservableProperty]
    private LineDto? selectedLine;

    /// <summary>Chỉ cho chọn/đổi Line khi đang tạo kế hoạch mới — kế hoạch đã tồn tại không cho đổi Line (server không hỗ trợ, xem class remarks).</summary>
    public bool CanEditLine => EditingId is null;

    partial void OnEditingIdChanged(int? value) => OnPropertyChanged(nameof(CanEditLine));

    partial void OnSelectedLineChanged(LineDto? value) => LineId = value?.Id ?? default;

    [ObservableProperty]
    private string customer = string.Empty;

    [ObservableProperty]
    private string model = string.Empty;

    [ObservableProperty]
    private string lot = string.Empty;

    [ObservableProperty]
    private string revision = string.Empty;

    [ObservableProperty]
    private int plannedQuantity;

    [ObservableProperty]
    private decimal taktTimeSeconds;

    /// <summary>
    /// Kiểu <see cref="DateTime"/>? để khớp đúng <c>DatePicker.SelectedDate</c> (tránh lỗi convert ngầm của WPF
    /// binding giữa DateTime/DateTime?). Lưu ý: DatePicker chỉ chọn NGÀY, chưa có ô chọn giờ — khác mockup gốc
    /// (2 ô Ngày + Giờ riêng) — cần bổ sung ô giờ ở bản sau để đúng đủ FR-05 "ngày + giờ".
    /// </summary>
    [ObservableProperty]
    private DateTime? startTime = DateTime.Today;

    [ObservableProperty]
    private string operatorNames = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Sản lượng chuẩn/giờ = 3600 / Takt time (FR-06) — chỉ để hiển thị ngay khi nhập, giá trị thật do server tính lúc lưu.</summary>
    public decimal StandardQuantityPerHour => TaktTimeSeconds > 0 ? Math.Round(3600m / TaktTimeSeconds, 2) : 0;

    partial void OnTaktTimeSecondsChanged(decimal value) => OnPropertyChanged(nameof(StandardQuantityPerHour));

    private readonly int _defaultLineId;

    public PlanSettingsViewModel(IProductionPlanApiClient apiClient, ILineApiClient lineApiClient, StationOptions options)
    {
        _apiClient = apiClient;
        _lineApiClient = lineApiClient;
        _defaultLineId = options.LineId;
        LineId = options.LineId;
    }

    /// <summary>Nạp danh mục Line (US-01) cho combobox "Line áp dụng" (AC1a) — gọi 1 lần lúc vào trang. KHÔNG lọc
    /// theo <c>IsActive</c> ở đây (AC1b) — việc lọc chỉ áp dụng khi tính <see cref="AvailableLines"/>.</summary>
    [RelayCommand]
    private async Task LoadLinesAsync()
    {
        StatusMessage = string.Empty;
        try
        {
            var items = await _lineApiClient.GetAllAsync();
            AllLines = new ObservableCollection<LineDto>(items);
            RecomputeAvailableLines();

            if (EditingId is not null && SelectedPlan is not null)
            {
                SelectedLine = AllLines.FirstOrDefault(l => l.Id == SelectedPlan.LineId);
            }
            else if (SelectedLine is null)
            {
                // Mặc định chọn sẵn Line của trạm đang đăng nhập khi vào trang lần đầu (giữ hành vi cũ trước AC1a,
                // lúc đó LineId luôn khởi tạo = options.LineId).
                SelectedLine = AllLines.FirstOrDefault(l => l.Id == _defaultLineId);
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
    }

    /// <summary>Tính lại danh sách hiển thị trong combobox (AC1a): chỉ Line đang hoạt động, cộng thêm Line đang
    /// gán cho <see cref="SelectedPlan"/> hiện tại dù đã vô hiệu hóa (AC1b) để không mất tên hiển thị.</summary>
    private void RecomputeAvailableLines()
    {
        var currentPlanLineId = SelectedPlan?.LineId;
        var items = AllLines
            .Where(l => l.IsActive || l.Id == currentPlanLineId)
            .OrderBy(l => l.Name);
        AvailableLines = new ObservableCollection<LineDto>(items);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var items = await _apiClient.GetAllAsync();
            Plans = new ObservableCollection<ProductionPlanDto>(items);
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
    private void New()
    {
        SelectedPlan = null;
        EditingId = null;
        FormTitle = "Tạo kế hoạch mới";
        SelectedLine = null;
        RecomputeAvailableLines();
        Customer = string.Empty;
        Model = string.Empty;
        Lot = string.Empty;
        Revision = string.Empty;
        PlannedQuantity = 0;
        TaktTimeSeconds = 0;
        StartTime = DateTime.Today;
        OperatorNames = string.Empty;
        StatusMessage = string.Empty;
    }

    partial void OnSelectedPlanChanged(ProductionPlanDto? value)
    {
        if (value is null)
        {
            return;
        }

        EditingId = value.Id;
        FormTitle = $"Sửa kế hoạch: {value.Lot}";
        LineId = value.LineId;
        RecomputeAvailableLines();
        SelectedLine = AllLines.FirstOrDefault(l => l.Id == value.LineId);
        Customer = value.Customer;
        Model = value.Model;
        Lot = value.Lot;
        Revision = value.Revision ?? string.Empty;
        PlannedQuantity = value.PlannedQuantity;
        TaktTimeSeconds = value.TaktTimeSeconds;
        StartTime = value.StartTime;
        OperatorNames = value.OperatorNames;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            if (EditingId is null)
            {
                var created = await _apiClient.CreateAsync(new CreateProductionPlanRequest
                {
                    LineId = LineId,
                    Customer = Customer,
                    Model = Model,
                    Lot = Lot,
                    Revision = string.IsNullOrWhiteSpace(Revision) ? null : Revision,
                    PlannedQuantity = PlannedQuantity,
                    TaktTimeSeconds = TaktTimeSeconds,
                    StartTime = StartTime ?? DateTime.Today,
                    OperatorNames = OperatorNames,
                });
                Plans.Add(created);
                StatusMessage = $"✓ Đã tạo kế hoạch {created.Lot}.";
                New();
            }
            else
            {
                await UpdateWithConfirmRetryAsync(EditingId.Value, confirm: false);
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

    private async Task UpdateWithConfirmRetryAsync(int id, bool confirm)
    {
        var request = new UpdateProductionPlanRequest
        {
            Customer = Customer,
            Model = Model,
            Lot = Lot,
            Revision = string.IsNullOrWhiteSpace(Revision) ? null : Revision,
            PlannedQuantity = PlannedQuantity,
            TaktTimeSeconds = TaktTimeSeconds,
            StartTime = StartTime ?? DateTime.Today,
            OperatorNames = OperatorNames,
            Confirm = confirm,
        };

        try
        {
            var updated = await _apiClient.UpdateAsync(id, request);
            var index = Plans.ToList().FindIndex(p => p.Id == id);
            if (index >= 0)
            {
                Plans[index] = updated;
            }

            StatusMessage = $"✓ Đã lưu kế hoạch {updated.Lot}.";
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict && !confirm)
        {
            var proceed = MessageBox.Show(
                ex.Message + "\n\nXác nhận lưu thay đổi?",
                "Cần xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;

            if (proceed)
            {
                await UpdateWithConfirmRetryAsync(id, confirm: true);
            }
        }
    }
}
