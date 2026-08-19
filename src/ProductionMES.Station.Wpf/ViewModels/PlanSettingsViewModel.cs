using System.Collections.ObjectModel;
using System.Globalization;
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
using ProductionMES.Station.Wpf.Services.Reports;

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
/// <remarks>
/// Sửa lại 17/08/2026 (US-05 AC1d/AC1e): "Thời gian bắt đầu" tách thành 2 ô nhập độc lập —
/// <see cref="StartDate"/> (DatePicker, chỉ Ngày) và <see cref="StartTimeOfDay"/> (TextBox, chuỗi "HH:mm") — ghép
/// lại thành <c>DateTime</c> đầy đủ chỉ lúc Lưu (<see cref="TryParseStartTime"/>), không giữ property
/// <c>StartTime</c> trung gian để tránh nhầm lẫn giữa giá trị đang gõ dở và giá trị đã ghép hợp lệ. "Takt time"
/// đổi từ nhập số giây thô sang <see cref="TaktTimeDisplay"/> (chuỗi "m:ss") — <see cref="TaktTimeSeconds"/> vẫn
/// giữ làm giá trị gửi API, tự đồng bộ 1 chiều khi gõ hợp lệ (<see cref="OnTaktTimeDisplayChanged"/>) để
/// <see cref="StandardQuantityPerHour"/> cập nhật ngay (AC3), nhưng giá trị AUTHORITATIVE thật sự gửi API luôn
/// được parse lại từ đầu lúc Lưu (<see cref="TaktTimeFormat.TryParse"/>) — nhập sai định dạng thì từ chối lưu qua
/// <see cref="StatusMessage"/>, không tự sửa ngầm (theo đúng yêu cầu AC1e).
/// </remarks>
public partial class PlanSettingsViewModel : ObservableObject
{
    private readonly IProductionPlanApiClient _apiClient;
    private readonly ILineApiClient _lineApiClient;
    private readonly ILotReportApiClient _lotReportApiClient;

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

    /// <summary>Giá trị giây gửi API — nguồn xác thực thật sự chỉ được cập nhật lúc Lưu (đã parse hợp lệ từ
    /// <see cref="TaktTimeDisplay"/>); trong lúc gõ dở được đồng bộ tạm ngay khi hợp lệ (<see cref="OnTaktTimeDisplayChanged"/>)
    /// chỉ để <see cref="StandardQuantityPerHour"/> cập nhật realtime (AC3).</summary>
    [ObservableProperty]
    private decimal taktTimeSeconds;

    /// <summary>Chuỗi hiển thị/nhập Takt time dạng "m:ss" (US-05 AC1e) — thay cho nhập số giây thô.</summary>
    [ObservableProperty]
    private string taktTimeDisplay = "0:00";

    partial void OnTaktTimeDisplayChanged(string value)
    {
        // Chỉ đồng bộ tạm khi hợp lệ để preview StandardQuantityPerHour cập nhật realtime (AC3); giá trị gửi API
        // thật sự luôn được parse lại từ đầu lúc Lưu (TryParseTaktTime) — không dựa vào giá trị tạm này.
        if (TaktTimeFormat.TryParse(value, out var seconds, out _))
        {
            TaktTimeSeconds = seconds;
        }
    }

    /// <summary>Ô chọn Ngày bắt đầu (US-05 AC1d) — ghép với <see cref="StartTimeOfDay"/> thành DateTime đầy đủ lúc Lưu.</summary>
    [ObservableProperty]
    private DateTime? startDate = DateTime.Today;

    /// <summary>Ô nhập Giờ:Phút bắt đầu, định dạng 24h "HH:mm" (US-05 AC1d).</summary>
    [ObservableProperty]
    private string startTimeOfDay = "00:00";

    [ObservableProperty]
    private string operatorNames = string.Empty;

    /// <summary>
    /// US-05 AC7 (=US-21a AC1) — "Tổng số lượng Lot", nhập tay, dùng chung 1 nguồn duy nhất cho mọi kế hoạch cùng
    /// Lot. <c>null</c> = chưa nhập/chưa xác định. Tự động điền lại khi gõ/chọn 1 Lot đã tồn tại (AC2/AC9, xem
    /// <see cref="LoadLotInfoAsync"/>) — vẫn sửa được tự do.
    /// </summary>
    [ObservableProperty]
    private int? lotTotalQuantity;

    /// <summary>AC7: true khi Lot đang gõ HOÀN TOÀN MỚI (chưa từng có kế hoạch nào trước đó) — hiển thị hint bắt buộc nhập <see cref="LotTotalQuantity"/>.</summary>
    [ObservableProperty]
    private bool isLotNew;

    /// <summary>AC9: breakdown "đã chạy OK theo từng (Line, Công đoạn)" của Lot đang gõ/chọn — rỗng nếu Lot mới hoặc chưa tra cứu.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLotBreakdownRows))]
    private ObservableCollection<LotStageRowDto> lotBreakdownRows = new();

    public bool HasLotBreakdownRows => LotBreakdownRows.Count > 0;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Sản lượng chuẩn/giờ = 3600 / Takt time (FR-06) — chỉ để hiển thị ngay khi nhập, giá trị thật do server tính lúc lưu.</summary>
    public decimal StandardQuantityPerHour => TaktTimeSeconds > 0 ? Math.Round(3600m / TaktTimeSeconds, 2) : 0;

    partial void OnTaktTimeSecondsChanged(decimal value) => OnPropertyChanged(nameof(StandardQuantityPerHour));

    private readonly int _defaultLineId;

    public PlanSettingsViewModel(IProductionPlanApiClient apiClient, ILineApiClient lineApiClient, ILotReportApiClient lotReportApiClient, StationOptions options)
    {
        _apiClient = apiClient;
        _lineApiClient = lineApiClient;
        _lotReportApiClient = lotReportApiClient;
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
        TaktTimeDisplay = "0:00";
        StartDate = DateTime.Today;
        StartTimeOfDay = "00:00";
        OperatorNames = string.Empty;
        LotTotalQuantity = null;
        IsLotNew = false;
        LotBreakdownRows = new ObservableCollection<LotStageRowDto>();
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
        TaktTimeDisplay = TaktTimeFormat.ToDisplay(value.TaktTimeSeconds);
        StartDate = value.StartTime.Date;
        StartTimeOfDay = value.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        OperatorNames = value.OperatorNames;
        // AC2 (US-21a): hiển thị lại tự động "Tổng số lượng Lot" hiện có của kế hoạch đang mở để sửa.
        LotTotalQuantity = value.LotTotalQuantity;
        IsLotNew = false;
        _ = LoadLotInfoAsync();
        StatusMessage = string.Empty;
    }

    /// <summary>
    /// US-05 AC9 (=US-21a AC4/AC9): gọi khi Tổ trưởng rời khỏi ô "Lot" (LostFocus) hoặc khi mở kế hoạch cũ để
    /// sửa — tra cứu Lot qua báo cáo Lot-centric (tái dùng <c>GET api/v1/reports/lots/{lot}</c>, KHÔNG gọi API
    /// mới): Lot chưa từng tồn tại (404) -> <see cref="IsLotNew"/> = true (bắt buộc nhập AC7); Lot đã tồn tại ->
    /// điền lại "Tổng số lượng Lot" hiện có (AC2, không ép nhập lại) + breakdown đã chạy OK (AC9). Lỗi mạng/server
    /// tạm thời KHÔNG chặn nhập liệu (cùng idiom gợi ý lý do NG, US-18 AC4) — chỉ bỏ qua âm thầm.
    /// </summary>
    [RelayCommand]
    private async Task LoadLotInfoAsync()
    {
        var lotCode = Lot?.Trim();
        LotBreakdownRows = new ObservableCollection<LotStageRowDto>();

        if (string.IsNullOrWhiteSpace(lotCode))
        {
            IsLotNew = false;
            return;
        }

        try
        {
            var summary = await _lotReportApiClient.GetSummaryAsync(lotCode);
            if (summary is null)
            {
                // AC7: Lot hoàn toàn mới -> bắt buộc nhập "Tổng số lượng Lot".
                IsLotNew = true;
                return;
            }

            IsLotNew = false;
            LotTotalQuantity = summary.LotTotalQuantity;
            LotBreakdownRows = new ObservableCollection<LotStageRowDto>(summary.Rows);
        }
        catch (ApiException)
        {
            // Không chặn nhập liệu vì lỗi tải gợi ý — Tổ trưởng vẫn có thể tự nhập/lưu, server vẫn validate lại.
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    /// <summary>Parse <see cref="StartDate"/> + <see cref="StartTimeOfDay"/> thành DateTime đầy đủ (US-05 AC1d) —
    /// gọi lúc Lưu, KHÔNG tự sửa ngầm nếu Giờ:Phút nhập sai định dạng.</summary>
    private bool TryParseStartTime(out DateTime value, out string? error)
    {
        value = default;
        error = null;

        if (StartDate is null)
        {
            error = "Vui lòng chọn Ngày bắt đầu.";
            return false;
        }

        if (!DateTime.TryParseExact(StartTimeOfDay?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOfDay))
        {
            error = "Giờ bắt đầu không hợp lệ, nhập theo định dạng HH:mm (24h), ví dụ \"07:30\".";
            return false;
        }

        value = StartDate.Value.Date + timeOfDay.TimeOfDay;
        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = string.Empty;

        // Validate format Takt time (AC1e) và Thời gian bắt đầu (AC1d) TRƯỚC khi gọi API — nhập sai thì từ chối
        // lưu, báo lỗi rõ ràng qua StatusMessage, không tự sửa ngầm giá trị.
        if (!TaktTimeFormat.TryParse(TaktTimeDisplay, out var taktTimeSecondsValue, out var taktTimeError))
        {
            StatusMessage = taktTimeError!;
            return;
        }

        if (!TryParseStartTime(out var startTimeValue, out var startTimeError))
        {
            StatusMessage = startTimeError!;
            return;
        }

        // US-05 AC7 (=US-21a AC1): chặn sớm phía client khi Lot HOÀN TOÀN MỚI (IsLotNew — cập nhật qua
        // LoadLotInfoAsync, LostFocus ô Lot) mà chưa nhập "Tổng số lượng Lot" — tránh gọi API rồi hiện nhầm popup
        // "Xác nhận lưu thay đổi?" (dành cho AC8 soft-confirm, KHÔNG áp dụng cho rule bắt buộc này — AC7 không có
        // đường Confirm để bỏ qua). Server vẫn validate lại (409) nếu IsLotNew chưa kịp cập nhật (vd Lưu ngay sau
        // khi gõ Lot mà chưa rời ô).
        if (IsLotNew && LotTotalQuantity is null)
        {
            StatusMessage = $"Lot \"{Lot}\" hoàn toàn mới — bắt buộc nhập \"Tổng số lượng Lot\" trước khi lưu kế hoạch.";
            return;
        }

        TaktTimeSeconds = taktTimeSecondsValue;

        IsBusy = true;
        try
        {
            if (EditingId is null)
            {
                await CreateWithConfirmRetryAsync(taktTimeSecondsValue, startTimeValue, confirm: false);
            }
            else
            {
                await UpdateWithConfirmRetryAsync(EditingId.Value, taktTimeSecondsValue, startTimeValue, confirm: false);
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

    /// <summary>
    /// US-05 AC7/AC8 (=US-21a AC1/AC3): tạo kế hoạch mới, kèm "Tổng số lượng Lot" — server có thể từ chối 409 khi
    /// (a) Lot hoàn toàn mới mà thiếu <see cref="LotTotalQuantity"/> (AC7 — KHÔNG có đường Confirm để bỏ qua,
    /// chỉ có thể sửa bằng cách nhập giá trị), hoặc (b) Lot đã tồn tại nhưng giảm "Tổng số lượng Lot" xuống dưới
    /// thực tế đã chạy (AC8 — soft-confirm, cùng UX 409-retry đã có ở <see cref="UpdateWithConfirmRetryAsync"/>).
    /// </summary>
    private async Task CreateWithConfirmRetryAsync(decimal taktTimeSecondsValue, DateTime startTimeValue, bool confirm)
    {
        var request = new CreateProductionPlanRequest
        {
            LineId = LineId,
            Customer = Customer,
            Model = Model,
            Lot = Lot,
            Revision = string.IsNullOrWhiteSpace(Revision) ? null : Revision,
            PlannedQuantity = PlannedQuantity,
            TaktTimeSeconds = taktTimeSecondsValue,
            StartTime = startTimeValue,
            OperatorNames = OperatorNames,
            LotTotalQuantity = LotTotalQuantity,
            Confirm = confirm,
        };

        try
        {
            var created = await _apiClient.CreateAsync(request);
            Plans.Add(created);
            StatusMessage = $"✓ Đã tạo kế hoạch {created.Lot}.";
            New();
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
                await CreateWithConfirmRetryAsync(taktTimeSecondsValue, startTimeValue, confirm: true);
            }
        }
    }

    private async Task UpdateWithConfirmRetryAsync(int id, decimal taktTimeSecondsValue, DateTime startTimeValue, bool confirm)
    {
        var request = new UpdateProductionPlanRequest
        {
            Customer = Customer,
            Model = Model,
            Lot = Lot,
            Revision = string.IsNullOrWhiteSpace(Revision) ? null : Revision,
            PlannedQuantity = PlannedQuantity,
            TaktTimeSeconds = taktTimeSecondsValue,
            StartTime = startTimeValue,
            OperatorNames = OperatorNames,
            LotTotalQuantity = LotTotalQuantity,
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
                await UpdateWithConfirmRetryAsync(id, taktTimeSecondsValue, startTimeValue, confirm: true);
            }
        }
    }
}
