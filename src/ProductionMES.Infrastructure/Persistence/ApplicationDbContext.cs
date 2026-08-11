using Microsoft.EntityFrameworkCore;

namespace ProductionMES.Infrastructure.Persistence;

/// <summary>
/// DbContext chính của hệ thống (EF Core, MySQL qua Pomelo).
/// Chưa khai báo DbSet cụ thể nào ở bước scaffold này — sẽ bổ sung khi có thiết kế entity chi tiết.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
