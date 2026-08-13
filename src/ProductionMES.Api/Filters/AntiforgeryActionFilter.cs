using System.Linq;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductionMES.Api.Authentication;

namespace ProductionMES.Api.Filters;

/// <summary>
/// Action filter chống CSRF (ADR-003) — bổ sung cho <c>SameSite=Strict</c> làm lớp phòng thủ thứ 2. Với mọi
/// request <c>POST</c>/<c>PUT</c> xác thực qua cookie (scheme "Bearer"/JWT mặc định, dùng cho web-admin),
/// validate header <c>X-CSRF-TOKEN</c> (client lấy trước qua <c>GET api/v1/auth/csrf</c>) khớp với cookie
/// <c>XSRF-TOKEN</c> đã cấp. Request <c>GET</c>... không có side-effect nên bỏ qua kiểm tra này.
/// </summary>
/// <remarks>
/// ADR-005: endpoint xác thực qua scheme "StationApiKey" (Station.Wpf, vd <c>POST api/v1/scans</c>) KHÔNG dùng
/// cookie — CSRF lợi dụng trình duyệt tự động gửi cookie kèm request nền, không áp dụng khi không dùng cookie —
/// nên bỏ qua kiểm tra CSRF cho các endpoint này (nhận diện qua <see cref="IAuthorizeData.AuthenticationSchemes"/>
/// khai báo ở <c>[Authorize(AuthenticationSchemes = StationApiKeyDefaults.AuthenticationScheme)]</c>).
/// </remarks>
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

        if (UsesStationApiKeyScheme(context))
        {
            await next();
            return;
        }

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

    /// <summary>
    /// Kiểm tra endpoint đang xử lý có khai báo <c>[Authorize(AuthenticationSchemes = "StationApiKey")]</c>
    /// hay không — endpoint đó không dùng cookie nên không cần/không có CSRF token.
    /// </summary>
    private static bool UsesStationApiKeyScheme(ActionExecutingContext context)
    {
        var authorizeData = context.HttpContext.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>();
        if (authorizeData is null)
        {
            return false;
        }

        return authorizeData.Any(a => a.AuthenticationSchemes?
            .Split(',')
            .Select(s => s.Trim())
            .Contains(StationApiKeyDefaults.AuthenticationScheme) == true);
    }
}
