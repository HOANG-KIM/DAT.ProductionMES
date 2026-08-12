using Microsoft.AspNetCore.Authorization;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Api.Authorization;

/// <summary>
/// Requirement cho custom policy-based authorization (ADR-004) — 1 policy tương ứng đúng 1 cặp
/// (<see cref="Resource"/>, <see cref="Action"/>), đăng ký ở <c>Program.cs</c> qua tên hằng số ở
/// <see cref="PermissionPolicies"/>.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(PermissionResource resource, PermissionAction action)
    {
        Resource = resource;
        Action = action;
    }

    public PermissionResource Resource { get; }

    public PermissionAction Action { get; }
}
