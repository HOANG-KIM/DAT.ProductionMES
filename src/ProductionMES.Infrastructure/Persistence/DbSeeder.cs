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

            // US-10 AC2/AC3: tra cứu lịch sử scan.
            (PermissionResource.Scan, PermissionAction.View),
            // US-18 (thay đổi yêu cầu 18/08/2026): xác nhận Scan NG tại trạm.
            (PermissionResource.Scan, PermissionAction.ConfirmNg),
            // US-19 AC2/AC6: "Mở khóa rework" cho tem bị NG — chỉ Tổ trưởng/Admin.
            (PermissionResource.Scan, PermissionAction.ReworkUnlock),

            // US-21: báo cáo tổng hợp ACTUAL/PLAN/BALANCE theo Line/công đoạn.
            (PermissionResource.Report, PermissionAction.View),

            // US-24: cấu hình Quy cách đóng gói theo Model — Admin (web-admin) + Supervisor (Station.Wpf), AC6.
            (PermissionResource.PackingModelConfig, PermissionAction.View),
            (PermissionResource.PackingModelConfig, PermissionAction.Create),
            (PermissionResource.PackingModelConfig, PermissionAction.Update),
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
            // ở 2 Controller đó) + Scan (US-10 — Tổ trưởng tra cứu lịch sử scan) + Report (US-21 — Tổ trưởng xem
            // báo cáo tổng hợp theo Line/công đoạn).
            var newSupervisorPermissions = newPermissions.Where(p =>
                p.Resource == PermissionResource.ProductionPlan || p.Resource == PermissionResource.ProductionPlanStage
                || p.Resource == PermissionResource.Scan || p.Resource == PermissionResource.Report
                || p.Resource == PermissionResource.PackingModelConfig);
            rolePermissions.AddRange(newSupervisorPermissions.Select(p => new RolePermission { Role = UserRole.Supervisor, PermissionId = p.Id }));

            // Manager: Scan.View (US-10, tra cứu lịch sử scan) + Scan.ConfirmNg (US-18, thay đổi 18/08/2026, xác
            // nhận Scan NG tại trạm — quyết định nghiệp vụ chốt 18/08/2026: mở rộng quyền xác nhận NG cho cả
            // Manager, không chỉ Supervisor) + Report.View (US-21, báo cáo tổng hợp — đúng vai trò "Ban quản lý/
            // Văn phòng" của US-21) — Manager KHÔNG có permission nào khác ngoài 2 nhóm Scan/Report. Scan.ReworkUnlock
            // (US-19) KHÔNG cấp cho Manager (khác 2 permission Scan trên) — AC6 chốt "chỉ Tổ trưởng" (+ Admin) mới
            // được "Mở khóa rework", không mở rộng cho Manager như đã làm với ConfirmNg.
            var newManagerPermissions = newPermissions.Where(p =>
                (p.Resource == PermissionResource.Scan && p.Action != PermissionAction.ReworkUnlock)
                || p.Resource == PermissionResource.Report);
            rolePermissions.AddRange(newManagerPermissions.Select(p => new RolePermission { Role = UserRole.Manager, PermissionId = p.Id }));

            // Operator: không permission nào (khớp thực tế hiện tại — chưa endpoint nào cho phép role này).

            dbContext.RolePermissions.AddRange(rolePermissions);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureSupervisorCatalogViewPermissionsAsync(dbContext, cancellationToken);
        await EnsureScanViewPermissionGrantsAsync(dbContext, cancellationToken);
        await EnsureScanConfirmNgPermissionGrantsAsync(dbContext, cancellationToken);
        await EnsureScanReworkUnlockPermissionGrantsAsync(dbContext, cancellationToken);
        await EnsureReportViewPermissionGrantsAsync(dbContext, cancellationToken);
        await EnsurePackingModelConfigPermissionGrantsAsync(dbContext, cancellationToken);
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

    /// <summary>
    /// Bổ sung 18/08/2026 (US-10 AC2/AC3, tra cứu lịch sử scan): đảm bảo <c>(Scan, View)</c> được cấp cho
    /// <c>Supervisor</c>/<c>Admin</c>/<c>Manager</c> ngay cả khi DB đã seed từ trước lúc permission này được thêm
    /// vào <c>catalog</c> ở <see cref="SeedPermissionsAsync"/> — cùng lý do/idiom với
    /// <see cref="EnsureSupervisorCatalogViewPermissionsAsync"/> (đoạn seed dựa trên <c>newPermissions</c> chỉ
    /// chạy đúng 1 lần khi permission còn "mới", không tự chạy lại cho DB đã seed trước đó). Idempotent theo từng
    /// cặp (Role, PermissionId), chạy MỖI lần khởi động.
    /// </summary>
    private static async Task EnsureScanViewPermissionGrantsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var scanViewPermissionId = await dbContext.Permissions
            .Where(p => p.Resource == PermissionResource.Scan && p.Action == PermissionAction.View)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (scanViewPermissionId is null)
        {
            return;
        }

        var roles = new[] { UserRole.Admin, UserRole.Supervisor, UserRole.Manager };

        var alreadyGrantedRoles = await dbContext.RolePermissions
            .Where(rp => rp.PermissionId == scanViewPermissionId.Value && roles.Contains(rp.Role))
            .Select(rp => rp.Role)
            .ToListAsync(cancellationToken);

        var missingRoles = roles.Except(alreadyGrantedRoles).ToList();
        if (missingRoles.Count == 0)
        {
            return;
        }

        dbContext.RolePermissions.AddRange(missingRoles.Select(role => new RolePermission { Role = role, PermissionId = scanViewPermissionId.Value }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Bổ sung 18/08/2026 (US-18, thay đổi yêu cầu — xác nhận Scan NG tại trạm bắt buộc đăng nhập Tổ trưởng):
    /// đảm bảo <c>(Scan, ConfirmNg)</c> được cấp cho <c>Supervisor</c>/<c>Admin</c>/<c>Manager</c> — cùng lý
    /// do/idiom phòng gap seed với <see cref="EnsureScanViewPermissionGrantsAsync"/> (dù ở lần đầu triển khai
    /// permission này thường đã được seed đủ qua nhánh <c>newPermissions</c> ở <see cref="SeedPermissionsAsync"/>,
    /// giữ bước "Ensure" riêng này làm lưới an toàn cho các kịch bản DB lệch pha khác nhau). Idempotent theo từng
    /// cặp (Role, PermissionId), chạy MỖI lần khởi động.
    /// </summary>
    private static async Task EnsureScanConfirmNgPermissionGrantsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var scanConfirmNgPermissionId = await dbContext.Permissions
            .Where(p => p.Resource == PermissionResource.Scan && p.Action == PermissionAction.ConfirmNg)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (scanConfirmNgPermissionId is null)
        {
            return;
        }

        var roles = new[] { UserRole.Admin, UserRole.Supervisor, UserRole.Manager };

        var alreadyGrantedRoles = await dbContext.RolePermissions
            .Where(rp => rp.PermissionId == scanConfirmNgPermissionId.Value && roles.Contains(rp.Role))
            .Select(rp => rp.Role)
            .ToListAsync(cancellationToken);

        var missingRoles = roles.Except(alreadyGrantedRoles).ToList();
        if (missingRoles.Count == 0)
        {
            return;
        }

        dbContext.RolePermissions.AddRange(missingRoles.Select(role => new RolePermission { Role = role, PermissionId = scanConfirmNgPermissionId.Value }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Bổ sung 18/08/2026 (US-19 AC2/AC6, "Mở khóa rework"): đảm bảo <c>(Scan, ReworkUnlock)</c> được cấp cho
    /// <c>Supervisor</c>/<c>Admin</c> — KHÔNG cấp <c>Manager</c> (khác <c>Scan.ConfirmNg</c>/<c>Scan.View</c>, xem
    /// AC6 "chỉ Tổ trưởng có quyền mở khóa") — cùng lý do/idiom lưới an toàn với
    /// <see cref="EnsureScanConfirmNgPermissionGrantsAsync"/>. Idempotent theo từng cặp (Role, PermissionId), chạy
    /// MỖI lần khởi động.
    /// </summary>
    private static async Task EnsureScanReworkUnlockPermissionGrantsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var scanReworkUnlockPermissionId = await dbContext.Permissions
            .Where(p => p.Resource == PermissionResource.Scan && p.Action == PermissionAction.ReworkUnlock)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (scanReworkUnlockPermissionId is null)
        {
            return;
        }

        var roles = new[] { UserRole.Admin, UserRole.Supervisor };

        var alreadyGrantedRoles = await dbContext.RolePermissions
            .Where(rp => rp.PermissionId == scanReworkUnlockPermissionId.Value && roles.Contains(rp.Role))
            .Select(rp => rp.Role)
            .ToListAsync(cancellationToken);

        var missingRoles = roles.Except(alreadyGrantedRoles).ToList();
        if (missingRoles.Count == 0)
        {
            return;
        }

        dbContext.RolePermissions.AddRange(missingRoles.Select(role => new RolePermission { Role = role, PermissionId = scanReworkUnlockPermissionId.Value }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Bổ sung 18/08/2026 (US-21, báo cáo tổng hợp theo Line/công đoạn): đảm bảo <c>(Report, View)</c> được cấp
    /// cho <c>Supervisor</c>/<c>Admin</c>/<c>Manager</c> — cùng lý do/idiom lưới an toàn với
    /// <see cref="EnsureScanViewPermissionGrantsAsync"/>. Idempotent theo từng cặp (Role, PermissionId), chạy MỖI
    /// lần khởi động.
    /// </summary>
    private static async Task EnsureReportViewPermissionGrantsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var reportViewPermissionId = await dbContext.Permissions
            .Where(p => p.Resource == PermissionResource.Report && p.Action == PermissionAction.View)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (reportViewPermissionId is null)
        {
            return;
        }

        var roles = new[] { UserRole.Admin, UserRole.Supervisor, UserRole.Manager };

        var alreadyGrantedRoles = await dbContext.RolePermissions
            .Where(rp => rp.PermissionId == reportViewPermissionId.Value && roles.Contains(rp.Role))
            .Select(rp => rp.Role)
            .ToListAsync(cancellationToken);

        var missingRoles = roles.Except(alreadyGrantedRoles).ToList();
        if (missingRoles.Count == 0)
        {
            return;
        }

        dbContext.RolePermissions.AddRange(missingRoles.Select(role => new RolePermission { Role = role, PermissionId = reportViewPermissionId.Value }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Bổ sung 20/08/2026 (US-24/FR-24, cấu hình Quy cách đóng gói theo Model): đảm bảo cả 3 permission
    /// <c>(PackingModelConfig, View/Create/Update)</c> được cấp cho <c>Admin</c>/<c>Supervisor</c> (AC6 — quản lý
    /// được từ cả web-admin lẫn Station.Wpf) — cùng lý do/idiom lưới an toàn với
    /// <see cref="EnsureReportViewPermissionGrantsAsync"/>. Idempotent theo từng cặp (Role, PermissionId), chạy
    /// MỖI lần khởi động.
    /// </summary>
    private static async Task EnsurePackingModelConfigPermissionGrantsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var permissionIds = await dbContext.Permissions
            .Where(p => p.Resource == PermissionResource.PackingModelConfig)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (permissionIds.Count == 0)
        {
            return;
        }

        var roles = new[] { UserRole.Admin, UserRole.Supervisor };

        var alreadyGranted = (await dbContext.RolePermissions
                .Where(rp => permissionIds.Contains(rp.PermissionId) && roles.Contains(rp.Role))
                .Select(rp => new { rp.Role, rp.PermissionId })
                .ToListAsync(cancellationToken))
            .Select(rp => (rp.Role, rp.PermissionId))
            .ToHashSet();

        var missing = roles
            .SelectMany(role => permissionIds.Select(permissionId => (Role: role, PermissionId: permissionId)))
            .Where(pair => !alreadyGranted.Contains(pair))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.RolePermissions.AddRange(missing.Select(pair => new RolePermission { Role = pair.Role, PermissionId = pair.PermissionId }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
