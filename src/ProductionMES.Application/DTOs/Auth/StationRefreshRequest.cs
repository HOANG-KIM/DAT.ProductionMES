namespace ProductionMES.Application.DTOs.Auth;

/// <summary>Request refresh token cho luồng Station.Wpf (ADR-005) — token gửi trong body, không có cookie để tự đọc.</summary>
public class StationRefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
