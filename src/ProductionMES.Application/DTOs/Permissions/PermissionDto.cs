using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.DTOs.Permissions;

/// <summary>DTO trả về cho client, đại diện 1 permission trong catalog cố định (ADR-004).</summary>
public class PermissionDto
{
    public int Id { get; set; }

    public PermissionResource Resource { get; set; }

    public PermissionAction Action { get; set; }
}
