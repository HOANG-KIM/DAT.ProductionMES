using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductionMES.Api.Filters;

/// <summary>
/// Action filter chống CSRF (ADR-003) — bổ sung cho <c>SameSite=Strict</c> làm lớp phòng thủ thứ 2. Với mọi
/// request <c>POST</c>/<c>PUT</c>, validate header <c>X-CSRF-TOKEN</c> (client lấy trước qua
/// <c>GET api/v1/auth/csrf</c>) khớp với cookie <c>XSRF-TOKEN</c> đã cấp. Request <c>GET</c>... không có
/// side-effect nên bỏ qua kiểm tra này.
/// </summary>
public class AntiforgeryActionFilter : IAsyncActionFilter
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryActionFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method))
        {
            try
            {
                await _antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "CSRF token không hợp lệ.",
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                };
                return;
            }
        }

        await next();
    }
}
