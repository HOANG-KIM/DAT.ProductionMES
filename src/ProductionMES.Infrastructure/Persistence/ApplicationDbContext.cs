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

    public DbSet<Stage> Stages => Set<Stage>();

    public DbSet<WorkStation> WorkStations => Set<WorkStation>();

    public DbSet<ProductionPlan> ProductionPlans => Set<ProductionPlan>();

    public DbSet<ProductionPlanStage> ProductionPlanStages => Set<ProductionPlanStage>();

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
