using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;
using ProductionMES.Station.Wpf.Services.Http;

namespace ProductionMES.Station.Wpf.Services.PackingModelConfigs;

/// <inheritdoc cref="IPackingModelConfigApiClient"/>
public class PackingModelConfigApiClient : ApiClientBase, IPackingModelConfigApiClient
{
    private const string BasePath = "api/v1/packing-model-configs";

    public PackingModelConfigApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public async Task<IReadOnlyList<PackingModelConfigDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BasePath);
        return await SendAsync<IReadOnlyList<PackingModelConfigDto>>(request, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> SuggestModelsAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrWhiteSpace(search) ? string.Empty : $"?search={Uri.EscapeDataString(search)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BasePath}/suggest-models{query}");
        return await SendAsync<IReadOnlyList<string>>(request, cancellationToken);
    }

    public async Task<PackingModelConfigDto> CreateAsync(CreatePackingModelConfigRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BasePath)
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        return await SendAsync<PackingModelConfigDto>(httpRequest, cancellationToken);
    }

    public async Task<PackingModelConfigDto> UpdateAsync(int id, UpdatePackingModelConfigRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"{BasePath}/{id}")
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options),
        };
        return await SendAsync<PackingModelConfigDto>(httpRequest, cancellationToken);
    }

    /// <summary>
    /// AC4 — multipart/form-data (KHÔNG qua <see cref="ApiClientBase.SendAsync{TResponse}"/>, vốn chỉ phục vụ
    /// JSON) — trường form tên "file", khớp tên tham số <c>IFormFile? file</c> ở
    /// <c>PackingModelConfigsController.UploadTemplate</c> (ASP.NET Core model binding theo tên).
    /// </summary>
    public async Task<PackingModelConfigDto> UploadTemplateAsync(int id, string filePath, CancellationToken cancellationToken = default)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        using var content = new MultipartFormDataContent();
        content.Add(streamContent, "file", Path.GetFileName(filePath));

        using var response = await HttpClient.PostAsync($"{BasePath}/{id}/template", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ToApiExceptionAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<PackingModelConfigDto>(JsonDefaults.Options, cancellationToken);
        return result ?? throw new ApiException(response.StatusCode, "Server trả về dữ liệu rỗng ngoài dự kiến.");
    }

    /// <summary>AC5 — tải file nhị phân (KHÔNG qua <see cref="ApiClientBase.SendAsync{TResponse}"/> vì response không phải JSON), ghi trực tiếp ra <paramref name="destinationFilePath"/>.</summary>
    public async Task DownloadTemplateAsync(int id, string destinationFilePath, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync($"{BasePath}/{id}/template", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ToApiExceptionAsync(response, cancellationToken);
        }

        await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await httpStream.CopyToAsync(fileStream, cancellationToken);
    }
}
