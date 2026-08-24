using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Application.Abstractions.Auth;
using ProductionMES.Application.Abstractions.Authorization;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Security;
using ProductionMES.Application.Abstractions.Storage;
using ProductionMES.Infrastructure.Persistence;
using ProductionMES.Infrastructure.Security;
using ProductionMES.Infrastructure.Storage;

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

        // US-04a/ADR-005: sinh/hash API Key theo trạm — cùng pattern IJwtTokenGenerator (interface ở
        // Application, implementation cụ thể ở Infrastructure).
        services.AddScoped<IApiKeyGenerator, ApiKeyGenerator>();

        // ADR-004: cache in-memory tra cứu RolePermission — dùng ở PermissionAuthorizationHandler (mỗi request)
        // và AuthService (danh sách permission trong response đăng nhập/refresh). AddMemoryCache là idempotent
        // (an toàn gọi nhiều lần) nên không cần kiểm tra đã đăng ký hay chưa.
        services.AddMemoryCache();
        services.AddScoped<IRolePermissionCache, RolePermissionCache>();

        // US-24: lưu file mẫu tem in (template .xlsx) trên filesystem server — interface ở Application, implementation
        // cụ thể ở Infrastructure (cùng pattern IApiKeyGenerator/IJwtTokenGenerator). PackingTemplateStorageOptions.BasePath
        // được Api resolve thành đường dẫn tuyệt đối lúc khởi động (xem Program.cs) trước khi bind vào Options pattern.
        services.AddScoped<IPackingTemplateStorage, PackingTemplateFileStorage>();

        return services;
    }
}
