using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Api.ExceptionHandling;

/// <summary>
/// Xử lý exception toàn cục cho toàn bộ API (dùng cơ chế <see cref="IExceptionHandler"/> chuẩn của ASP.NET Core 8),
/// chuyển exception chưa được Controller/Service bắt riêng thành <see cref="ProblemDetails"/>.
/// Đây là hạ tầng dùng chung cho mọi feature sau này — thêm ở US-01 vì là feature đầu tiên chạm tới Controller.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Lỗi chưa được xử lý khi xử lý request {Path}", httpContext.Request.Path);

        var statusCode = exception switch
        {
            EntityNotFoundException => StatusCodes.Status404NotFound,
            BusinessRuleException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        var title = statusCode switch
        {
            StatusCodes.Status404NotFound => "Không tìm thấy dữ liệu",
            StatusCodes.Status409Conflict => "Vi phạm quy tắc nghiệp vụ",
            _ => "Đã xảy ra lỗi hệ thống",
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
