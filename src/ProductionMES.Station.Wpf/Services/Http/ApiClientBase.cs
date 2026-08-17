using System.Net.Http;
using System.Net.Http.Json;
using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Http;

/// <summary>
/// Hạ tầng dùng chung cho mọi API client (Production Plan, Production Plan Stage...) — gửi request, đọc lỗi
/// theo chuẩn <c>ProblemDetails</c> (API-Conventions.md mục 6) và ném <see cref="ApiException"/> với message
/// tiếng Việt sẵn sàng hiển thị, thay vì để mỗi client tự try/catch lặp lại logic này.
/// </summary>
public abstract class ApiClientBase
{
    protected readonly HttpClient HttpClient;

    protected ApiClientBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    protected async Task<TResponse> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ToApiExceptionAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonDefaults.Options, cancellationToken);
        return result ?? throw new ApiException(response.StatusCode, "Server trả về dữ liệu rỗng ngoài dự kiến.");
    }

    protected async Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ToApiExceptionAsync(response, cancellationToken);
        }
    }

    private static async Task<ApiException> ToApiExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return new ApiException(response.StatusCode, "Phiên đăng nhập Tổ trưởng đã hết hạn hoặc chưa đăng nhập — vui lòng đăng nhập lại.");
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(JsonDefaults.Options, cancellationToken);
            if (problem is not null)
            {
                var message = problem.Errors is { Count: > 0 }
                    ? string.Join(" ", problem.Errors.SelectMany(e => e.Value))
                    : (problem.Detail ?? problem.Title);
                return new ApiException(response.StatusCode, message, problem.Errors);
            }
        }
        catch
        {
            // Body không phải ProblemDetails hợp lệ (vd lỗi hạ tầng ngoài dự kiến) -> rơi xuống message mặc định bên dưới.
        }

        return new ApiException(response.StatusCode, $"Lỗi không xác định từ server (HTTP {(int)response.StatusCode}).");
    }
}
