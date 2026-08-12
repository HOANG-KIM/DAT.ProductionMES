using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductionMES.Application.Options;
using ProductionMES.Application.Services.Auth;
using ProductionMES.Application.Services.Permissions;
using ProductionMES.Application.Services.Stages;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Application.Services.ProductionPlans;
using ProductionMES.Application.Services.Lines;
using ProductionMES.Application.Services.Users;
using ProductionMES.Application.Services.WorkStations;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Application.DependencyInjection;

/// <summary>
/// Đăng ký các service của tầng Application (Service, Validator...) vào DI container.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<ILineService, LineService>();
        services.AddScoped<IStageService, StageService>();
        services.AddScoped<IWorkStationService, WorkStationService>();
        services.AddScoped<IProductionPlanService, ProductionPlanService>();
        services.AddScoped<IProductionPlanStageService, ProductionPlanStageService>();

        // US-22: PasswordHasher<TUser> (Microsoft.Extensions.Identity.Core) — không cần cả hệ thống
        // ASP.NET Core Identity đầy đủ, chỉ dùng đúng phần băm/kiểm tra mật khẩu.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}
