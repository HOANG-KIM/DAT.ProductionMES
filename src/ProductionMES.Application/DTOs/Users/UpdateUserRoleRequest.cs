using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Users;

/// <summary>Request cập nhật vai trò 1 tài khoản đã tồn tại (US-22/AC1). Chỉ Admin được thực hiện.</summary>
public class UpdateUserRoleRequest
{
    public UserRole UserRole { get; set; }
}
