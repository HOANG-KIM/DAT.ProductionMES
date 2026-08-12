using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMES.Application.DTOs.Users;
using ProductionMES.Application.Services.Users;

namespace ProductionMES.Api.Controllers;

/// <summary>Quản lý tài khoản người dùng & phân quyền (US-22/FR-22). Chỉ Admin được thao tác (AC1).</summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Lấy danh sách toàn bộ tài khoản.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>Lấy chi tiết 1 tài khoản theo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Tạo mới 1 tài khoản, kèm gán vai trò (AC1).</summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật vai trò 1 tài khoản (AC1).</summary>
    [HttpPut("{id:int}/role")]
    public async Task<ActionResult<UserDto>> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var updated = await _userService.UpdateUserRoleAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Vô hiệu hóa tài khoản — soft-delete qua cờ hoạt động, không xóa cứng dữ liệu.</summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _userService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
