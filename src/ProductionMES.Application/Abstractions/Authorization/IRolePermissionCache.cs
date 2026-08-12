using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Abstractions.Authorization;

/// <summary>
/// Tra cứu permission hiệu lực của 1 role (ADR-004), có cache — dùng ở
/// <c>PermissionAuthorizationHandler</c> (mỗi request cần xác thực) và <c>AuthService</c> (trả danh sách
/// permission vào response đăng nhập/refresh). Đặt tại tầng Application (không phải Infrastructure) để Service
/// có thể phụ thuộc vào abstraction này mà không reference ngược sang Infrastructure — implementation cụ thể
/// (in-memory cache, đọc DB qua IUnitOfWork) nằm ở Infrastructure, cùng pattern với IRepository/IUnitOfWork.
/// </summary>
public interface IRolePermissionCache
{
    /// <summary>Role <paramref name="role"/> có đang được cấp permission (<paramref name="resource"/>, <paramref name="action"/>) hay không.</summary>
    Task<bool> HasPermissionAsync(UserRole role, PermissionResource resource, PermissionAction action, CancellationToken cancellationToken = default);

    /// <summary>Danh sách permission hiệu lực của <paramref name="role"/>, dạng chuỗi <c>"{Resource}.{Action}"</c> (vd. <c>"Line.View"</c>).</summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa cache hiện tại — lần gọi tiếp theo tới <see cref="HasPermissionAsync"/>/<see cref="GetPermissionsAsync"/>
    /// sẽ tự nạp lại từ DB. Phải gọi ngay sau khi API cấp/thu hồi permission ghi DB thành công, để thay đổi có
    /// hiệu lực ngay (không chờ TTL cache hết hạn).
    /// </summary>
    void Invalidate();
}
