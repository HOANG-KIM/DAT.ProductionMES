using Microsoft.AspNetCore.Authorization;
using ProductionMES.Application.Abstractions.Authorization;
using ProductionMES.Domain.Enums;
using ProductionMES.Infrastructure.Security;

namespace ProductionMES.Api.Authorization;

/// <summary>
/// Xử lý <see cref="PermissionRequirement"/> (ADR-004) — đọc role từ claim <c>"role"</c>
/// (<see cref="JwtTokenGenerator.RoleClaimType"/>) trong JWT hiện có của request, tra cứu permission qua
/// <see cref="IRolePermissionCache"/> (cache, không query DB mỗi request).
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRolePermissionCache _rolePermissionCache;

    public PermissionAuthorizationHandler(IRolePermissionCache rolePermissionCache)
    {
        _rolePermissionCache = rolePermissionCache;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roleClaimValue = context.User.FindFirst(JwtTokenGenerator.RoleClaimType)?.Value;
        if (roleClaimValue is null || !Enum.TryParse<UserRole>(roleClaimValue, out var role))
        {
            return;
        }

        var hasPermission = await _rolePermissionCache.HasPermissionAsync(role, requirement.Resource, requirement.Action);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
