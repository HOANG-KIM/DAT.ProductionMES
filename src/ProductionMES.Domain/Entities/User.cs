using ProductionMES.Domain.Enums;

namespace ProductionMES.Domain.Entities;

/// <summary>Tài khoản người dùng hệ thống, gắn 1 trong 4 vai trò (US-22/FR-22).</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>Tên đăng nhập, duy nhất trong hệ thống.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Mật khẩu đã băm (hash) bằng <c>Microsoft.AspNetCore.Identity.PasswordHasher&lt;User&gt;</c> — không lưu plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Họ tên hiển thị.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Vai trò của tài khoản — quyết định phạm vi chức năng được phép truy cập (AC1).</summary>
    public UserRole UserRole { get; set; }

    /// <summary>
    /// Trạng thái hoạt động. Vô hiệu hóa (soft-delete) chỉ đổi cờ này thành <c>false</c>, không xóa cứng —
    /// giữ nguyên lịch sử thao tác gắn với tài khoản (vd log mở khóa rework, người xác nhận NG...).
    /// </summary>
    public bool IsActive { get; set; } = true;
}
