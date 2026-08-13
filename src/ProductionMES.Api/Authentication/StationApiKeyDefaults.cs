namespace ProductionMES.Api.Authentication;

/// <summary>Hằng số dùng chung cho AuthenticationScheme "StationApiKey" (US-04a, ADR-005).</summary>
public static class StationApiKeyDefaults
{
    /// <summary>Tên scheme đăng ký ở Program.cs, dùng trong <c>[Authorize(AuthenticationSchemes = ...)]</c>.</summary>
    public const string AuthenticationScheme = "StationApiKey";

    /// <summary>Tên header client gửi API key thô lên.</summary>
    public const string HeaderName = "X-Station-Api-Key";

    /// <summary>
    /// Claim type chứa <c>WorkStationId</c> của trạm đã xác thực thành công — Controller dùng claim này (đọc
    /// từ danh tính đã xác thực, không chỉ tin request body) để đối chiếu chống giả danh khi cần (ADR-005 dòng 84).
    /// </summary>
    public const string WorkStationIdClaimType = "station_work_station_id";
}
