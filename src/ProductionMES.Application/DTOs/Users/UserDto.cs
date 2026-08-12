using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Users;

/// <summary>DTO trả về cho client, đại diện 1 tài khoản người dùng (US-22). Không lộ PasswordHash ra ngoài.</summary>
public class UserDto
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole UserRole { get; set; }

    public bool IsActive { get; set; }
}
