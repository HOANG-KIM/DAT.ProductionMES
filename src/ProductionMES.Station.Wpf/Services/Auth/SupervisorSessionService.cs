namespace ProductionMES.Station.Wpf.Services.Auth;

/// <inheritdoc cref="ISupervisorSessionService"/>
public class SupervisorSessionService : ISupervisorSessionService
{
    public bool IsAuthenticated { get; private set; }

    public string? AccessToken { get; private set; }

    public string? RefreshToken { get; private set; }

    public string? Username { get; private set; }

    public string? FullName { get; private set; }

    public event EventHandler? SessionChanged;

    public void SetSession(string accessToken, DateTime accessTokenExpiresAtUtc, string refreshToken, DateTime refreshTokenExpiresAtUtc, string username, string fullName)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        Username = username;
        FullName = fullName;
        IsAuthenticated = true;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        Username = null;
        FullName = null;
        IsAuthenticated = false;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
