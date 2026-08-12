using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Application.Abstractions.Auth;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Infrastructure.Persistence;
using ProductionMES.Infrastructure.Security;

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

        // US-22: implementation cụ thể (System.IdentityModel.Tokens.Jwt) đặt ở Infrastructure, interface ở
        // Application — cùng pattern với IRepository/IUnitOfWork.
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
