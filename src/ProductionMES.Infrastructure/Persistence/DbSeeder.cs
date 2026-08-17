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
    /// + <c>ProductionPlanStage</c>; <c>Operator</c>/<c>Manager</c> không có permission nào. Idempotent theo
    /// từng cặp (Resource, Action) — chỉ chèn phần chưa tồn tại, để lần chạy sau khi bổ sung entry mới vào
    /// <c>catalog</c> (vd US-01a/US-04a) vẫn seed đúng trên 1 DB đã seed từ trước, thay vì bị chặn hoàn toàn
    /// vì bảng <see cref="ApplicationDbContext.Permissions"/> không còn rỗng.
    /// </summary>
    public static async Task SeedPermissionsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var catalog = new[]
        {
            (PermissionResource.Line, PermissionAction.View),
            (PermissionResource.Line, PermissionAction.Create),
            (PermissionResource.Line, PermissionAction.Update),
            (PermissionResource.Line, PermissionAction.Deactivate),

            (PermissionResource.BreakWindow, PermissionAction.View),
            (PermissionResource.BreakWindow, PermissionAction.Create),
            (PermissionResource.BreakWindow, PermissionAction.Update),
            (PermissionResource.BreakWindow, PermissionAction.Delete),

            (PermissionResource.StationApiKey, PermissionAction.View),
            (PermissionResource.StationApiKey, PermissionAction.Create),
            (PermissionResource.StationApiKey, PermissionAction.Update),
            (PermissionResource.StationApiKey, PermissionAction.Deactivate),

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

            (PermissionResource.ProductionPlanStage, PermissionAction.View),
            (PermissionResource.ProductionPlanStage, PermissionAction.Create),
            (PermissionResource.ProductionPlanStage, PermissionAction.Update),
            (PermissionResource.ProductionPlanStage, PermissionAction.Delete),
            // US-05a: vòng đời trạng thái theo cặp (Kế hoạch, Công đoạn) — thay cho ProductionPlan.Activate/
            // Deactivate cấp cả kế hoạch trước đây.
            (PermissionResource.ProductionPlanStage, PermissionAction.Apply),
            (PermissionResource.ProductionPlanStage, PermissionAction.Pause),
            (PermissionResource.ProductionPlanStage, PermissionAction.Close),
        };

        var existingPairs = (await dbContext.Permissions
                .Select(p => new { p.Resource, p.Action })
                .ToListAsync(cancellationToken))
            .Select(p => (p.Resource, p.Action))
            .ToHashSet();

        var newPermissions = catalog
            .Where(c => !existingPairs.Contains((c.Item1, c.Item2)))
            .Select(c => new Permission { Resource = c.Item1, Action = c.Item2 })
            .ToList();

        if (newPermissions.Count > 0)
        {
            dbContext.Permissions.AddRange(newPermissions);
            await dbContext.SaveChangesAsync(cancellationToken);

            var rolePermissions = new List<RolePermission>();

            // Admin: toàn bộ permission MỚI (khớp hardcode Admin cũ ở LinesController/StagesController/
            // WorkStationsController/ProductionPlansController/ProductionPlanStagesController). Permission đã tồn
            // tại từ trước giữ nguyên RolePermission đã gán, không tạo trùng.
            rolePermissions.AddRange(newPermissions.Select(p => new RolePermission { Role = UserRole.Admin, PermissionId = p.Id }));

            // Supervisor: phần MỚI của ProductionPlan + ProductionPlanStage (khớp hardcode "Supervisor,Admin" cũ
            // ở 2 Controller đó).
            var newSupervisorPermissions = newPermissions.Where(p =>
                p.Resource == PermissionResource.ProductionPlan || p.Resource == PermissionResource.ProductionPlanStage);
            rolePermissions.AddRange(newSupervisorPermissions.Select(p => new RolePermission { Role = UserRole.Supervisor, PermissionId = p.Id }));

            // Operator/Manager: không permission nào (khớp thực tế hiện tại — chưa endpoint nào cho phép 2 role này).

            dbContext.RolePermissions.AddRange(rolePermissions);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureSupervisorCatalogViewPermissionsAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// Bổ sung 17/08/2026 (rà soát gap "gõ tay Id Line/Công đoạn" ở <c>Station.Wpf</c>, US-05 AC1a/US-03 AC8):
    /// Tổ trưởng nâng quyền tại trạm (<c>Station.Wpf</c>) cần gọi <c>GET api/v1/lines</c> (combobox chọn Line ở
    /// <c>PlanSettingsPage</c>) và <c>GET api/v1/stages</c> (combobox chọn Công đoạn ở <c>LineStageSequencePage</c>,
    /// đã dùng từ trước nhưng CHƯA từng seed đủ quyền) — 2 permission này KHÔNG nằm trong nhóm "MỚI" ở
    /// <see cref="SeedPermissionsAsync"/> (đã tồn tại từ catalog gốc 12/08/2026, chỉ seed cho <c>Admin</c>) nên
    /// đoạn seed dựa trên <c>newPermissions</c> ở trên không bao giờ chạy lại cho 2 permission này. Tách thành
    /// bước riêng, tự kiểm tra và bổ sung nếu thiếu (idempotent theo từng cặp Role+PermissionId), chạy MỖI lần
    /// khởi động bất kể catalog Permission có gì mới hay không.
    /// </summary>
    private static async Task EnsureSupervisorCatalogViewPermissionsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var resources = new[] { PermissionResource.Line, PermissionResource.Stage };

        var viewPermissionIds = await dbContext.Permissions
            .Where(p => resources.Contains(p.Resource) && p.Action == PermissionAction.View)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (viewPermissionIds.Count == 0)
        {
            return;
        }

        var alreadyGrantedIds = await dbContext.RolePermissions
            .Where(rp => rp.Role == UserRole.Supervisor && viewPermissionIds.Contains(rp.PermissionId))
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var missingIds = viewPermissionIds.Except(alreadyGrantedIds).ToList();
        if (missingIds.Count == 0)
        {
            return;
        }

        dbContext.RolePermissions.AddRange(missingIds.Select(id => new RolePermission { Role = UserRole.Supervisor, PermissionId = id }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
