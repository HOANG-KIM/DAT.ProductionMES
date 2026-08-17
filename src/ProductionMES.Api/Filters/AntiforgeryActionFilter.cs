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
///
/// Luồng Supervisor Bearer của Station.Wpf (<c>POST auth/station-login</c>/<c>station-refresh</c>/
/// <c>station-logout</c>) cũng không dùng cookie nhưng KHÔNG có <c>[Authorize]</c> (đây là bước đăng nhập, chưa
/// có token) nên không khớp điều kiện trên — các action này đánh dấu riêng bằng <c>[IgnoreAntiforgeryToken]</c>
/// (built-in, chỉ dùng làm marker vì filter tự viết tay, không dùng cơ chế antiforgery middleware mặc định).
///
/// Các endpoint <c>ProductionPlan*</c>/<c>ProductionPlanStage*</c> dùng CHUNG <c>[Authorize(Policy=...)]</c> với
/// scheme "Bearer" mặc định cho CẢ <c>web-admin</c> (cookie, cần CSRF) lẫn <c>Station.Wpf</c> (header
/// <c>Authorization: Bearer</c>, không cần CSRF) — không thể đánh dấu tĩnh theo action vì sẽ tắt luôn bảo vệ
/// CSRF của <c>web-admin</c>. Phải phân biệt NGAY TẠI TỪNG REQUEST: nếu request tự gắn header
/// <c>Authorization</c> tường minh (trình duyệt không thể tự động gắn header tuỳ ý qua tấn công CSRF — chỉ code
/// client chủ động mới gắn được), coi như không phải luồng cookie nên bỏ qua CSRF cho đúng request đó; ngược
/// lại (xác thực bằng cookie <c>access_token</c> tự động gửi kèm, không có header) vẫn bắt buộc CSRF như cũ.
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

        if (UsesStationApiKeyScheme(context) || IsMarkedIgnoreAntiforgery(context) || HasExplicitBearerHeader(request))
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

    /// <summary>Endpoint có gắn <c>[IgnoreAntiforgeryToken]</c> (vd luồng station-login/refresh/logout, ADR-005).</summary>
    private static bool IsMarkedIgnoreAntiforgery(ActionExecutingContext context) =>
        context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IgnoreAntiforgeryTokenAttribute>() is not null;

    /// <summary>
    /// Request tự gắn header <c>Authorization: Bearer ...</c> — đúng luồng Station.Wpf (ADR-005 mục 2), khác
    /// với web-admin (chỉ dựa vào cookie <c>access_token</c> tự động gửi kèm, không bao giờ tự gắn header này —
    /// xem API-Conventions.md mục 7). Trình duyệt không thể tự động gắn header tuỳ ý qua tấn công CSRF nên an
    /// toàn để bỏ qua kiểm tra CSRF khi có header này.
    /// </summary>
    private static bool HasExplicitBearerHeader(HttpRequest request) =>
        request.Headers.Authorization.Any(v => v is not null && v.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));
}
