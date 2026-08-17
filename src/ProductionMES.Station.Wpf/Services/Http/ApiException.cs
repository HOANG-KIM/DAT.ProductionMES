using System.Net;
using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Http;

/// <summary>
/// Lỗi gọi API đã được đọc/parse từ <see cref="ApiProblemDetails"/> — <see cref="Message"/> luôn là text tiếng
/// Việt sẵn sàng hiển thị thẳng cho Tổ trưởng (không cần UI tự suy diễn thêm theo status code).
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public Dictionary<string, string[]>? ValidationErrors { get; }

    public ApiException(HttpStatusCode statusCode, string message, Dictionary<string, string[]>? validationErrors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ValidationErrors = validationErrors;
    }
}
