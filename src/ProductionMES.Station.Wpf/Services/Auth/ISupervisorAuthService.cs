namespace ProductionMES.Station.Wpf.Services.Auth;

/// <summary>Đăng nhập/đăng xuất Supervisor tại trạm qua <c>POST auth/station-login</c>/<c>station-logout</c> (ADR-005).</summary>
public interface ISupervisorAuthService
{
    /// <summary>Đăng nhập; ném <see cref="Http.ApiException"/> nếu sai tài khoản/mật khẩu (401).</summary>
    Task LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>Thu hồi refresh token hiện có (nếu có) và xoá session cục bộ — luôn thành công từ góc nhìn UI.</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);
}
