using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Users;

/// <summary>Request tạo mới 1 tài khoản người dùng, kèm gán vai trò (US-22/AC1). Chỉ Admin được thực hiện.</summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole UserRole { get; set; }
}
