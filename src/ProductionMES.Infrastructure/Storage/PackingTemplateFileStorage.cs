using Microsoft.Extensions.Options;
using ProductionMES.Application.Abstractions.Storage;
using ProductionMES.Application.Options;

namespace ProductionMES.Infrastructure.Storage;

/// <inheritdoc cref="IPackingTemplateStorage"/>
/// <remarks>
/// File đặt tên <c>{packingModelConfigId}.xlsx</c> trong thư mục <see cref="PackingTemplateStorageOptions.BasePath"/>
/// (đường dẫn tuyệt đối, đã resolve ở tầng Api — xem <c>Program.cs</c>). Tự tạo thư mục nếu chưa tồn tại (idempotent),
/// tránh lỗi lần đầu chạy trên máy chưa có sẵn <c>App_Data/PackingTemplates</c> (thư mục không commit nội dung thật).
/// </remarks>
public class PackingTemplateFileStorage : IPackingTemplateStorage
{
    private readonly string _basePath;

    public PackingTemplateFileStorage(IOptions<PackingTemplateStorageOptions> options)
    {
        _basePath = options.Value.BasePath;
    }

    public async Task SaveAsync(int packingModelConfigId, Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_basePath);

        var filePath = GetFilePath(packingModelConfigId);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(int packingModelConfigId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(packingModelConfigId);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(int packingModelConfigId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(packingModelConfigId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetFilePath(int packingModelConfigId) => Path.Combine(_basePath, $"{packingModelConfigId}.xlsx");
}
