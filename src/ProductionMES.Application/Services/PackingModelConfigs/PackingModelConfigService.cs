using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Storage;
using ProductionMES.Application.DTOs.PackingModelConfigs;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.PackingModelConfigs;

/// <summary>
/// Implementation <see cref="IPackingModelConfigService"/> (US-24/FR-24). So khớp Model theo AC9 (không phân
/// biệt hoa/thường, tự trim khoảng trắng) dựa trên <see cref="PackingModelConfig.ModelNormalized"/> — snapshot
/// đã chuẩn hoá lưu sẵn lúc tạo, KHÔNG phụ thuộc collation thật của MySQL (cùng lý do <c>Lot.Code</c>).
/// </summary>
public class PackingModelConfigService : IPackingModelConfigService
{
    private const string XlsxExtension = ".xlsx";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPackingTemplateStorage _templateStorage;

    public PackingModelConfigService(IUnitOfWork unitOfWork, IPackingTemplateStorage templateStorage)
    {
        _unitOfWork = unitOfWork;
        _templateStorage = templateStorage;
    }

    public async Task<IReadOnlyList<PackingModelConfigDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.Repository<PackingModelConfig>().GetAllAsync(cancellationToken);
        return items.OrderBy(x => x.Model).Select(ToDto).ToList();
    }

    public async Task<PackingModelConfigDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var config = await _unitOfWork.Repository<PackingModelConfig>().GetByIdAsync(id, cancellationToken);
        return config is null ? null : ToDto(config);
    }

    public async Task<PackingModelConfigDto?> GetByModelAsync(string model, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(model);
        var config = (await _unitOfWork.Repository<PackingModelConfig>()
                .FindAsync(c => c.ModelNormalized == normalized, cancellationToken))
            .FirstOrDefault();
        return config is null ? null : ToDto(config);
    }

    public async Task<IReadOnlyList<string>> SuggestModelsAsync(string? search, CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.Repository<PackingModelConfig>().GetAllAsync(cancellationToken);
        var query = items.Select(x => x.Model).Distinct();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = Normalize(search);
            query = query.Where(m => Normalize(m).Contains(normalizedSearch));
        }

        return query.OrderBy(m => m).ToList();
    }

    public async Task<PackingModelConfigDto> CreateAsync(
        CreatePackingModelConfigRequest request, string? updatedByUserName, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request.Model);
        var existing = (await _unitOfWork.Repository<PackingModelConfig>()
                .FindAsync(c => c.ModelNormalized == normalized, cancellationToken))
            .FirstOrDefault();
        if (existing is not null)
        {
            throw new BusinessRuleException($"Model \"{request.Model}\" đã có cấu hình đóng gói (đã cấu hình dưới tên \"{existing.Model}\").");
        }

        var now = DateTime.UtcNow;
        var config = new PackingModelConfig
        {
            Model = request.Model.Trim(),
            ModelNormalized = normalized,
            PackingQuantity = request.PackingQuantity,
            GrossWeight = request.GrossWeight,
            PartName = request.PartName.Trim(),
            Manufacturer = string.IsNullOrWhiteSpace(request.Manufacturer) ? null : request.Manufacturer.Trim(),
            HasTemplate = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            UpdatedByUserName = updatedByUserName,
        };

        var repository = _unitOfWork.Repository<PackingModelConfig>();
        await repository.AddAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(config);
    }

    public async Task<PackingModelConfigDto> UpdateAsync(
        int id, UpdatePackingModelConfigRequest request, string? updatedByUserName, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<PackingModelConfig>();
        var config = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy cấu hình đóng gói với Id = {id}.");

        // AC2: chỉ cập nhật quy cách/khối lượng/tên sản phẩm/nhà sản xuất — KHÔNG đổi Model (khoá tra cứu, xem
        // UpdatePackingModelConfigRequest). Các thùng đã đóng/in tem trước đó không bị ảnh hưởng (không hồi tố,
        // US-25 xử lý phần snapshot — US-24 chỉ cần đọc ra giá trị mới sau khi sửa).
        config.PackingQuantity = request.PackingQuantity;
        config.GrossWeight = request.GrossWeight;
        config.PartName = request.PartName.Trim();
        config.Manufacturer = string.IsNullOrWhiteSpace(request.Manufacturer) ? null : request.Manufacturer.Trim();
        config.UpdatedAtUtc = DateTime.UtcNow;
        config.UpdatedByUserName = updatedByUserName;

        repository.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(config);
    }

    public async Task<PackingModelConfigDto> UploadTemplateAsync(
        int id, Stream content, string fileName, string? updatedByUserName, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<PackingModelConfig>();
        var config = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy cấu hình đóng gói với Id = {id}.");

        if (string.IsNullOrWhiteSpace(fileName) || !Path.GetExtension(fileName).Equals(XlsxExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Chỉ chấp nhận file mẫu tem định dạng .xlsx.");
        }

        if (content.Length == 0)
        {
            throw new BusinessRuleException("File mẫu tem đang tải lên rỗng, vui lòng chọn lại file.");
        }

        // AC4: lưu file, TỰ ĐỘNG thay thế file mẫu cũ (nếu có) — PackingTemplateFileStorage ghi đè theo đúng tên
        // file cố định (đặt tên theo Id, không dùng chuỗi Model), không cần bước xoá riêng.
        await _templateStorage.SaveAsync(id, content, cancellationToken);

        config.HasTemplate = true;
        config.TemplateUpdatedAtUtc = DateTime.UtcNow;
        config.TemplateUpdatedByUserName = updatedByUserName;
        config.UpdatedAtUtc = config.TemplateUpdatedAtUtc.Value;
        config.UpdatedByUserName = updatedByUserName;

        repository.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(config);
    }

    public async Task<(Stream Content, string FileName)> DownloadTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        var config = await _unitOfWork.Repository<PackingModelConfig>().GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy cấu hình đóng gói với Id = {id}.");

        if (!config.HasTemplate)
        {
            throw new EntityNotFoundException($"Model \"{config.Model}\" chưa có file mẫu tem nào được tải lên.");
        }

        var stream = await _templateStorage.OpenReadAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Model \"{config.Model}\" chưa có file mẫu tem nào được tải lên.");

        return (stream, $"mau-tem-{SanitizeFileName(config.Model)}{XlsxExtension}");
    }

    /// <summary>Thay mọi ký tự không hợp lệ cho tên file (Model là free-text, có thể chứa ký tự đặc biệt) bằng "_" — chỉ dùng để đặt tên file tải xuống, KHÔNG ảnh hưởng <see cref="PackingModelConfig.Model"/> lưu trong DB.</summary>
    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    /// <summary>AC9: chuẩn hoá tường minh (trim + upper invariant) — không phụ thuộc collation MySQL.</summary>
    private static string Normalize(string model) => model.Trim().ToUpperInvariant();

    private static PackingModelConfigDto ToDto(PackingModelConfig config) => new()
    {
        Id = config.Id,
        Model = config.Model,
        PackingQuantity = config.PackingQuantity,
        GrossWeight = config.GrossWeight,
        PartName = config.PartName,
        Manufacturer = config.Manufacturer,
        HasTemplate = config.HasTemplate,
        TemplateUpdatedAtUtc = config.TemplateUpdatedAtUtc,
        TemplateUpdatedByUserName = config.TemplateUpdatedByUserName,
        CreatedAtUtc = config.CreatedAtUtc,
        UpdatedAtUtc = config.UpdatedAtUtc,
        UpdatedByUserName = config.UpdatedByUserName,
    };
}
