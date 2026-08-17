namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>StationLoginResponse</c> phía backend (ADR-005) — token trả trong body, không cookie.</summary>
public class StationLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole UserRole { get; set; }

    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}
