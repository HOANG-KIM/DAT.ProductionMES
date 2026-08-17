namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>LoginRequest</c> phía backend — dùng cho POST auth/station-login (ADR-005).</summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
