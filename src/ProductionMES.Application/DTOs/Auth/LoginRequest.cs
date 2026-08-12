namespace ProductionMES.Application.DTOs.Auth;

/// <summary>Request đăng nhập (US-22).</summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
