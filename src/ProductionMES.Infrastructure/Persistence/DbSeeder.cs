using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Infrastructure.Persistence;

/// <summary>
/// Seed dữ liệu khởi tạo lúc ứng dụng khởi động (US-22) — hiện chỉ seed 1 tài khoản Admin mặc định để có thể
/// đăng nhập lần đầu và test end-to-end ngay, không cần thao tác tay trên DB.
/// <b>Cảnh báo bảo mật</b>: tài khoản/mật khẩu mặc định dưới đây CHỈ dùng cho môi trường dev/test ban đầu —
/// bắt buộc phải đổi mật khẩu (hoặc vô hiệu hóa tài khoản này và tạo tài khoản Admin khác) trước khi đưa vào
/// môi trường production thật.
/// </summary>
public static class DbSeeder
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Admin@123";

    public static async Task SeedDefaultAdminAsync(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher, CancellationToken cancellationToken = default)
    {
        var hasAnyUser = await dbContext.Users.AnyAsync(cancellationToken);
        if (hasAnyUser)
        {
            return;
        }

        var admin = new User
        {
            Username = DefaultAdminUsername,
            FullName = "Quản trị hệ thống (mặc định)",
            UserRole = UserRole.Admin,
            IsActive = true,
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, DefaultAdminPassword);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
