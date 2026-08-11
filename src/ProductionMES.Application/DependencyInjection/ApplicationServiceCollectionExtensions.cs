using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ProductionMES.Application.DependencyInjection;

/// <summary>
/// Đăng ký các service của tầng Application (Service, Validator...) vào DI container.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        return services;
    }
}
