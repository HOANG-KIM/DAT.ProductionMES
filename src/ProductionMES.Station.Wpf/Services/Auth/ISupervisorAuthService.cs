using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.Auth;

/// <summary>Đăng nhập/đăng xuất Supervisor tại trạm qua <c>POST auth/station-login</c>/<c>station-logout</c> (ADR-005).</summary>
public interface ISupervisorAuthService
{
    /// <summary>Đăng nhập; ném <see cref="Http.ApiException"/> nếu sai tài khoản/mật khẩu (401).</summary>
    Task LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>Thu hồi refresh token hiện có (nếu có) và xoá session cục bộ — luôn thành công từ góc nhìn UI.</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// US-18 (thay đổi yêu cầu 18/08/2026 — bấm nút "NG" bắt buộc đăng nhập lại Tổ trưởng NGAY LẬP TỨC, re-auth
    /// mỗi lần): đăng nhập RIÊNG cho 1 lượt Scan NG — GỌI CHUNG endpoint <c>station-login</c> như
    /// <see cref="LoginAsync"/>, nhưng KHÔNG ghi vào <see cref="ISupervisorSessionService"/> dùng chung (tránh ảnh
    /// hưởng phiên Tổ trưởng đang mở cho US-05/US-05a/US-05b, vốn dùng pattern "session còn hạn thì bỏ qua đăng
    /// nhập" — khác hẳn yêu cầu re-auth mỗi lần ở đây). Caller (<c>AndonBoardViewModel</c>) tự giữ
    /// <see cref="StationLoginResponse.AccessToken"/> trong bộ nhớ, dùng đúng 1 lần cho toàn bộ lượt Scan NG này
    /// (quét tem/nhập lý do/xác nhận — AC2a), rồi bỏ đi (không lưu lại cho lượt sau).
    /// </summary>
    Task<StationLoginResponse> LoginForNgConfirmationAsync(string username, string password, CancellationToken cancellationToken = default);
}
