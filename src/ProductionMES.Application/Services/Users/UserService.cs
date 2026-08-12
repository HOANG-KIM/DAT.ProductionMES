using Microsoft.AspNetCore.Identity;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Users;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.Users;

/// <summary>
/// Implementation IUserService (US-22/FR-22). Mật khẩu được băm ngay khi tạo tài khoản, không bao giờ
/// lưu/trả về plaintext. Vô hiệu hóa là soft-delete, cùng pattern với các danh mục khác.
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<User>();

        var existingUsername = await repository.FindAsync(u => u.Username == request.Username, cancellationToken);
        if (existingUsername.Count > 0)
        {
            throw new BusinessRuleException($"Tên đăng nhập \"{request.Username}\" đã tồn tại.");
        }

        var user = new User
        {
            Username = request.Username,
            FullName = request.FullName,
            UserRole = request.UserRole,
            IsActive = true,
        };
        // Băm mật khẩu sau khi có instance User (PasswordHasher<TUser> cần TUser để tính hash, dù không
        // đọc trực tiếp field nào khác ngoài mật khẩu truyền vào).
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await repository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto> UpdateUserRoleAsync(int id, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<User>();
        var user = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy người dùng với Id = {id}.");

        user.UserRole = request.UserRole;

        repository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var repository = _unitOfWork.Repository<User>();
        var user = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException($"Không tìm thấy người dùng với Id = {id}.");

        user.IsActive = false;

        repository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Repository<User>().GetAllAsync(cancellationToken);
        return users.Select(ToDto).ToList();
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        UserRole = user.UserRole,
        IsActive = user.IsActive,
    };
}
