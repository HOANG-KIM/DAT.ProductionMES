using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Application.DTOs.Permissions;
using ProductionMES.Application.Services.Permissions;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Api.Controllers;

/// <summary>
/// Quản lý catalog Permission và ma trận RolePermission (ADR-004) — break-glass, hardcode
/// <c>[Authorize(Roles = "Admin")]</c>, KHÔNG đi qua hệ thống permission động (đảm bảo luôn có đường quản trị
/// hoạt động được kể cả khi Admin cấu hình sai permission của chính role Admin cho các resource khác).
/// Route không dùng chung 1 tiền tố resource duy nhất (<c>permissions</c>, <c>role-permissions</c>,
/// <c>roles/{role}/permissions/{permissionId}</c>) nên khai báo route đầy đủ ở từng action thay vì
/// <c>[Route]</c> mức Controller.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>Catalog toàn bộ Permission hợp lệ trong hệ thống (cố định, không có API tạo/sửa/xóa).</summary>
    [HttpGet("api/v1/permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetCatalog(CancellationToken cancellationToken)
    {
        var permissions = await _permissionService.GetCatalogAsync(cancellationToken);
        return Ok(permissions);
    }

    /// <summary>Ma trận Role × Permission hiện tại — 1 dòng cho mỗi <see cref="UserRole"/>.</summary>
    [HttpGet("api/v1/role-permissions")]
    public async Task<ActionResult<IReadOnlyList<RolePermissionMatrixDto>>> GetRolePermissionMatrix(CancellationToken cancellationToken)
    {
        var matrix = await _permissionService.GetRolePermissionMatrixAsync(cancellationToken);
        return Ok(matrix);
    }

    /// <summary>Cấp 1 permission cho 1 role — idempotent, cấp lại permission đã có không lỗi.</summary>
    [HttpPost("api/v1/roles/{role}/permissions/{permissionId:int}")]
    public async Task<IActionResult> Grant(UserRole role, int permissionId, CancellationToken cancellationToken)
    {
        await _permissionService.GrantAsync(role, permissionId, cancellationToken);
        return NoContent();
    }

    /// <summary>Thu hồi 1 permission khỏi 1 role — idempotent, thu hồi permission chưa có không lỗi.</summary>
    [HttpDelete("api/v1/roles/{role}/permissions/{permissionId:int}")]
    public async Task<IActionResult> Revoke(UserRole role, int permissionId, CancellationToken cancellationToken)
    {
        await _permissionService.RevokeAsync(role, permissionId, cancellationToken);
        return NoContent();
    }
}
