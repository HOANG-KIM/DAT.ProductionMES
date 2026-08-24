using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;
using ProductionMES.Station.Wpf.Services.PackingModelConfigs;

namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// ViewModel màn "Cấu hình đóng gói theo Model" (US-24) — Tổ trưởng nâng quyền tại trạm quản lý CÙNG dữ liệu với
/// web-admin (AC6), qua đúng <c>api/v1/packing-model-configs</c>. Thao tác chọn/lưu file (OpenFileDialog/
/// SaveFileDialog) đặt ở code-behind (<c>PackingModelConfigPage.xaml.cs</c>, thuần UI) — ViewModel chỉ nhận vào
/// đường dẫn file cục bộ đã chọn sẵn, giữ ViewModel không phụ thuộc trực tiếp <c>Microsoft.Win32</c>.
/// </summary>
public partial class PackingModelConfigViewModel : ObservableObject
{
    private readonly IPackingModelConfigApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<PackingModelConfigDto> configs = new();

    [ObservableProperty]
    private PackingModelConfigDto? selectedConfig;

    /// <summary>Model đã có cấu hình — gợi ý autocomplete khi tạo mới (AC9).</summary>
    [ObservableProperty]
    private ObservableCollection<string> availableModels = new();

    [ObservableProperty]
    private string formTitle = "Thêm cấu hình đóng gói mới";

    /// <summary>Id của cấu hình đang sửa — <c>null</c> nghĩa là đang tạo mới.</summary>
    [ObservableProperty]
    private int? editingId;

    [ObservableProperty]
    private string model = string.Empty;

    /// <summary>AC2: Model KHÔNG đổi được sau khi tạo — chỉ cho gõ khi đang tạo mới.</summary>
    [ObservableProperty]
    private bool canEditModel = true;

    [ObservableProperty]
    private string packingQuantityText = string.Empty;

    [ObservableProperty]
    private string grossWeightText = string.Empty;

    [ObservableProperty]
    private string partName = string.Empty;

    [ObservableProperty]
    private string manufacturer = string.Empty;

    [ObservableProperty]
    private string templateStatusText = "Chưa lưu cấu hình nên chưa thể tải mẫu tem.";

    [ObservableProperty]
    private bool canManageTemplate;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public PackingModelConfigViewModel(IPackingModelConfigApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    partial void OnSelectedConfigChanged(PackingModelConfigDto? value)
    {
        if (value is not null)
        {
            LoadIntoForm(value);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var items = await _apiClient.GetAllAsync();
            Configs = new ObservableCollection<PackingModelConfigDto>(items.OrderBy(x => x.Model));
            AvailableModels = new ObservableCollection<string>(items.Select(x => x.Model).Distinct().OrderBy(x => x));
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

    /// <summary>Chuyển sang chế độ tạo mới — xoá trắng form (AC1).</summary>
    [RelayCommand]
    private void New()
    {
        SelectedConfig = null;
        LoadIntoForm(null);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        StatusMessage = string.Empty;

        if (!int.TryParse(PackingQuantityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var packingQuantity))
        {
            StatusMessage = "Quy cách đóng gói phải là số nguyên hợp lệ.";
            return;
        }

        decimal? grossWeight = null;
        if (!string.IsNullOrWhiteSpace(GrossWeightText))
        {
            if (!decimal.TryParse(GrossWeightText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedWeight))
            {
                StatusMessage = "Khối lượng phải là số hợp lệ.";
                return;
            }

            grossWeight = parsedWeight;
        }

        IsBusy = true;
        try
        {
            if (EditingId is int id)
            {
                var updated = await _apiClient.UpdateAsync(id, new UpdatePackingModelConfigRequest
                {
                    PackingQuantity = packingQuantity,
                    GrossWeight = grossWeight,
                    PartName = PartName,
                    Manufacturer = string.IsNullOrWhiteSpace(Manufacturer) ? null : Manufacturer,
                });
                StatusMessage = $"✓ Đã cập nhật cấu hình đóng gói cho Model \"{updated.Model}\".";
            }
            else
            {
                var created = await _apiClient.CreateAsync(new CreatePackingModelConfigRequest
                {
                    Model = Model,
                    PackingQuantity = packingQuantity,
                    GrossWeight = grossWeight,
                    PartName = PartName,
                    Manufacturer = string.IsNullOrWhiteSpace(Manufacturer) ? null : Manufacturer,
                });
                StatusMessage = $"✓ Đã tạo mới cấu hình đóng gói cho Model \"{created.Model}\".";
            }

            await LoadAsync();
            var savedModel = EditingId is int existingId
                ? Configs.FirstOrDefault(c => c.Id == existingId)
                : Configs.FirstOrDefault(c => c.Model == Model);
            if (savedModel is not null)
            {
                SelectedConfig = savedModel;
                LoadIntoForm(savedModel);
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

    /// <summary>AC4 — tải lên (thay thế) file mẫu tem cho cấu hình đang sửa, gọi từ code-behind sau khi người dùng chọn file qua OpenFileDialog.</summary>
    public async Task UploadTemplateAsync(string filePath)
    {
        if (EditingId is not int id)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var updated = await _apiClient.UploadTemplateAsync(id, filePath);
            LoadIntoForm(updated);
            var index = Configs.ToList().FindIndex(c => c.Id == id);
            if (index >= 0)
            {
                Configs[index] = updated;
            }

            StatusMessage = "✓ Đã tải lên mẫu tem mới.";
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

    /// <summary>AC5 — tải xuống file mẫu tem đang cấu hình, gọi từ code-behind sau khi người dùng chọn nơi lưu qua SaveFileDialog.</summary>
    public async Task DownloadTemplateAsync(string destinationFilePath)
    {
        if (EditingId is not int id)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            await _apiClient.DownloadTemplateAsync(id, destinationFilePath);
            StatusMessage = $"✓ Đã tải xuống mẫu tem vào \"{destinationFilePath}\".";
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

    private void LoadIntoForm(PackingModelConfigDto? config)
    {
        if (config is null)
        {
            FormTitle = "Thêm cấu hình đóng gói mới";
            EditingId = null;
            CanEditModel = true;
            Model = string.Empty;
            PackingQuantityText = string.Empty;
            GrossWeightText = string.Empty;
            PartName = string.Empty;
            Manufacturer = string.Empty;
            TemplateStatusText = "Chưa lưu cấu hình nên chưa thể tải mẫu tem.";
            CanManageTemplate = false;
            return;
        }

        FormTitle = $"Sửa cấu hình đóng gói — {config.Model}";
        EditingId = config.Id;
        CanEditModel = false; // AC2: Model không đổi được sau khi tạo.
        Model = config.Model;
        PackingQuantityText = config.PackingQuantity.ToString(CultureInfo.InvariantCulture);
        GrossWeightText = config.GrossWeight?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        PartName = config.PartName;
        Manufacturer = config.Manufacturer ?? string.Empty;
        TemplateStatusText = config.HasTemplate
            ? $"Đã có mẫu tem (cập nhật lần cuối bởi {config.TemplateUpdatedByUserName ?? "—"})"
            : "Chưa có mẫu tem nào được tải lên.";
        CanManageTemplate = true;
    }
}
