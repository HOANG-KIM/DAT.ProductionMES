using ProductionMES.Application.DTOs.Users;

namespace ProductionMES.Application.Services.Users;

/// <summary>Service quản lý tài khoản người dùng & phân quyền (US-22/FR-22). Chỉ Admin được thao tác.</summary>
public interface IUserService
{
    /// <summary>Tạo mới 1 tài khoản, kèm gán vai trò (AC1). Từ chối nếu tên đăng nhập đã tồn tại.</summary>
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật vai trò 1 tài khoản đã tồn tại (AC1).</summary>
    Task<UserDto> UpdateUserRoleAsync(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Vô hiệu hóa tài khoản (soft-delete qua cờ hoạt động, không xóa cứng bản ghi).</summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
