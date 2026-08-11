using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Infrastructure.Persistence;

namespace ProductionMES.Infrastructure.DependencyInjection;

/// <summary>
/// Đăng ký các service của tầng Infrastructure (DbContext, Unit of Work, Repository...) vào DI container.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(5, 7, 16))));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
