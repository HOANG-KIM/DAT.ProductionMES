namespace ProductionMES.Station.Wpf.Services.Auth;

/// <summary>
/// Giữ trạng thái đăng nhập Supervisor trong bộ nhớ tiến trình (ADR-005) — dùng chung cho cả
/// <c>AndonBoardWindow</c> và <c>MainWindow</c> trong cùng phiên chạy (ADR-006). KHÔNG ghi token ra file/registry.
/// </summary>
public interface ISupervisorSessionService
{
    bool IsAuthenticated { get; }

    string? AccessToken { get; }

    string? Username { get; }

    string? FullName { get; }

    /// <summary>Nâng lên trạng thái đã đăng nhập sau khi <c>station-login</c> thành công.</summary>
    void SetSession(string accessToken, DateTime accessTokenExpiresAtUtc, string refreshToken, DateTime refreshTokenExpiresAtUtc, string username, string fullName);

    /// <summary>Xoá toàn bộ token khỏi bộ nhớ — gọi khi Đăng xuất hoặc khi nhận 401 không thể phục hồi.</summary>
    void Clear();

    /// <summary>Refresh token thô hiện có (nếu cần gọi <c>station-refresh</c> sau này) — không lộ ra UI.</summary>
    string? RefreshToken { get; }

    /// <summary>Báo hiệu trạng thái đăng nhập vừa đổi (đăng nhập/đăng xuất) — để các Window/Page cùng cập nhật UI.</summary>
    event EventHandler? SessionChanged;
}
