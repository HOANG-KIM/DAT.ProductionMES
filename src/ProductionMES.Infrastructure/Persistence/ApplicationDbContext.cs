using Microsoft.EntityFrameworkCore;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Infrastructure.Persistence;

/// <summary>
/// DbContext chính của hệ thống (EF Core, MySQL qua Pomelo).
/// EF Core Migrations là nguồn schema duy nhất của toàn hệ thống (kể cả bảng chỉ dùng Dapper để query).
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Line> Lines => Set<Line>();

    public DbSet<BreakWindow> BreakWindows => Set<BreakWindow>();

    public DbSet<Stage> Stages => Set<Stage>();

    public DbSet<WorkStation> WorkStations => Set<WorkStation>();

    public DbSet<StationApiKey> StationApiKeys => Set<StationApiKey>();

    public DbSet<ProductionPlan> ProductionPlans => Set<ProductionPlan>();

    public DbSet<ProductionPlanStage> ProductionPlanStages => Set<ProductionPlanStage>();

    public DbSet<Lot> Lots => Set<Lot>();

    public DbSet<LotHistory> LotHistories => Set<LotHistory>();

    public DbSet<LineStageSequence> LineStageSequences => Set<LineStageSequence>();

    public DbSet<PackingModelConfig> PackingModelConfigs => Set<PackingModelConfig>();

    public DbSet<PackingBox> PackingBoxes => Set<PackingBox>();

    // US-27 (25/08/2026): DbSet<PackingDuplicateScanConfirmation> đã bị XÓA (bảng superseded bởi
    // Scan.ConfirmedByUserId/ConfirmedByUserName, xem US-27 AC12) — migration DropPackingDuplicateScanConfirmation
    // xóa bảng tương ứng khỏi schema.

    public DbSet<Scan> Scans => Set<Scan>();

    public DbSet<ReworkUnlock> ReworkUnlocks => Set<ReworkUnlock>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
