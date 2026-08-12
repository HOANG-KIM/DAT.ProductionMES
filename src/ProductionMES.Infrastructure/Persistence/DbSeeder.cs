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

    /// <summary>
    /// Seed catalog <see cref="Permission"/> (toàn bộ cặp Resource+Action hợp lệ, khớp đúng action thật đang
    /// tồn tại ở từng Controller — xem ADR-004) và <see cref="RolePermission"/> ban đầu, khớp chính xác hành vi
    /// hardcode <c>[Authorize(Roles = "...")]</c> trước khi có ADR-004 (không đổi behavior khi migrate):
    /// <c>Admin</c> được toàn bộ permission; <c>Supervisor</c> được toàn bộ permission của <c>ProductionPlan</c>
    /// + <c>ProductionPlanStage</c>; <c>Operator</c>/<c>Manager</c> không có permission nào. Idempotent — chỉ
    /// chạy nếu bảng <see cref="ApplicationDbContext.Permissions"/> đang rỗng.
    /// </summary>
    public static async Task SeedPermissionsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var hasAnyPermission = await dbContext.Permissions.AnyAsync(cancellationToken);
        if (hasAnyPermission)
        {
            return;
        }

        var catalog = new[]
        {
            (PermissionResource.Line, PermissionAction.View),
            (PermissionResource.Line, PermissionAction.Create),
            (PermissionResource.Line, PermissionAction.Update),
            (PermissionResource.Line, PermissionAction.Deactivate),

            (PermissionResource.Stage, PermissionAction.View),
            (PermissionResource.Stage, PermissionAction.Create),
            (PermissionResource.Stage, PermissionAction.Update),
            (PermissionResource.Stage, PermissionAction.Deactivate),

            (PermissionResource.WorkStation, PermissionAction.View),
            (PermissionResource.WorkStation, PermissionAction.Create),
            (PermissionResource.WorkStation, PermissionAction.Update),
            (PermissionResource.WorkStation, PermissionAction.Deactivate),

            (PermissionResource.ProductionPlan, PermissionAction.View),
            (PermissionResource.ProductionPlan, PermissionAction.Create),
            (PermissionResource.ProductionPlan, PermissionAction.Update),
            (PermissionResource.ProductionPlan, PermissionAction.Activate),
            (PermissionResource.ProductionPlan, PermissionAction.Deactivate),

            (PermissionResource.ProductionPlanStage, PermissionAction.View),
            (PermissionResource.ProductionPlanStage, PermissionAction.Create),
            (PermissionResource.ProductionPlanStage, PermissionAction.Update),
            (PermissionResource.ProductionPlanStage, PermissionAction.Delete),
        };

        var permissions = catalog
            .Select(c => new Permission { Resource = c.Item1, Action = c.Item2 })
            .ToList();

        dbContext.Permissions.AddRange(permissions);
        await dbContext.SaveChangesAsync(cancellationToken);

        var rolePermissions = new List<RolePermission>();

        // Admin: toàn bộ permission ở trên (khớp hardcode Admin cũ ở LinesController/StagesController/
        // WorkStationsController/ProductionPlansController/ProductionPlanStagesController).
        rolePermissions.AddRange(permissions.Select(p => new RolePermission { Role = UserRole.Admin, PermissionId = p.Id }));

        // Supervisor: toàn bộ permission của ProductionPlan + ProductionPlanStage (khớp hardcode
        // "Supervisor,Admin" cũ ở 2 Controller đó).
        var supervisorPermissions = permissions.Where(p =>
            p.Resource == PermissionResource.ProductionPlan || p.Resource == PermissionResource.ProductionPlanStage);
        rolePermissions.AddRange(supervisorPermissions.Select(p => new RolePermission { Role = UserRole.Supervisor, PermissionId = p.Id }));

        // Operator/Manager: không permission nào (khớp thực tế hiện tại — chưa endpoint nào cho phép 2 role này).

        dbContext.RolePermissions.AddRange(rolePermissions);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
