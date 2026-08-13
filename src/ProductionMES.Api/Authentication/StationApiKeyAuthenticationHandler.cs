using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductionMES.Application.Services.StationApiKeys;

namespace ProductionMES.Api.Authentication;

/// <summary>
/// AuthenticationHandler cho scheme "StationApiKey" (US-04a, ADR-005) — xác thực trạm (không phải người dùng)
/// qua header <see cref="StationApiKeyDefaults.HeaderName"/>. Chạy song song với scheme "Bearer" (JWT) hiện có,
/// KHÔNG đi qua hệ permission <c>Resource.Action</c> động (ADR-004) vì không có <c>User</c> nào gắn với request
/// dùng scheme này.
/// </summary>
/// <remarks>
/// AC6 (chống giả danh trạm): nếu body request là JSON và có field cấp cao <c>workStationId</c>, handler đọc
/// (buffered, không tiêu thụ stream — request body được reset lại vị trí đầu để model binding phía sau vẫn đọc
/// bình thường) và đối chiếu với trạm sở hữu API key ngay tại bước xác thực — endpoint nào có field này trong
/// body (vd tương lai <c>POST api/v1/scans</c>) tự động được bảo vệ mà không cần thêm logic riêng ở Controller.
/// Endpoint không có field này (vd GET, hoặc body không phải JSON) bỏ qua bước đối chiếu này ở tầng xác thực.
/// </remarks>
public class StationApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IStationApiKeyService _stationApiKeyService;

    public StationApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IStationApiKeyService stationApiKeyService)
        : base(options, logger, encoder)
    {
        _stationApiKeyService = stationApiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(StationApiKeyDefaults.HeaderName, out var headerValues))
        {
            return AuthenticateResult.Fail($"Thiếu header {StationApiKeyDefaults.HeaderName}.");
        }

        var rawApiKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return AuthenticateResult.Fail($"Header {StationApiKeyDefaults.HeaderName} rỗng.");
        }

        var expectedWorkStationId = await TryReadWorkStationIdFromBodyAsync();

        var authenticatedWorkStationId = await _stationApiKeyService.ValidateAsync(
            rawApiKey, expectedWorkStationId, Context.RequestAborted);

        if (authenticatedWorkStationId is null)
        {
            return AuthenticateResult.Fail("API Key không hợp lệ, đã bị thu hồi, hoặc không thuộc về trạm gửi trong request.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, authenticatedWorkStationId.Value.ToString()),
            new Claim(StationApiKeyDefaults.WorkStationIdClaimType, authenticatedWorkStationId.Value.ToString()),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    /// Đọc field JSON cấp cao <c>workStationId</c> từ request body nếu có (AC6) — không phá hỏng model binding
    /// phía sau vì reset lại <see cref="HttpRequest.Body"/> về vị trí đầu sau khi đọc xong.
    /// </summary>
    private async Task<int?> TryReadWorkStationIdFromBodyAsync()
    {
        if (!Request.Body.CanRead)
        {
            return null;
        }

        Request.EnableBuffering();
        Request.Body.Position = 0;

        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: Context.RequestAborted);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("workStationId", out var property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetInt32(out var workStationId))
            {
                return workStationId;
            }
        }
        catch (JsonException)
        {
            // Body rỗng hoặc không phải JSON hợp lệ (vd GET không có body) — bỏ qua bước đối chiếu AC6, để
            // Controller/model binding phía sau tự xử lý lỗi 400 nếu endpoint đó thực sự cần body JSON.
        }
        finally
        {
            Request.Body.Position = 0;
        }

        return null;
    }
}
