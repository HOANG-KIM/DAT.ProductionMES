using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProductionMES.Application.Abstractions.Authorization;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Infrastructure.Security;

/// <summary>
/// Implementation <see cref="IRolePermissionCache"/> (ADR-004) dựa trên <see cref="IMemoryCache"/>. Nạp toàn bộ
/// <see cref="RolePermission"/> (kèm <see cref="Permission"/> qua Include) từ DB qua <see cref="IUnitOfWork"/>,
/// gom thành <c>Dictionary&lt;UserRole, HashSet&lt;string&gt;&gt;</c> (key permission dạng
/// <c>"{Resource}.{Action}"</c>, khớp đúng format trả về ở <c>LoginResponse.Permissions</c>). 1 cache key cố
/// định, TTL dài (1 giờ) chỉ làm lưới an toàn — chủ yếu dựa vào <see cref="Invalidate"/> được gọi tường minh
/// ngay sau khi API cấp/thu hồi permission ghi DB thành công (xem <c>PermissionService</c>).
/// </summary>
public class RolePermissionCache : IRolePermissionCache
{
    private const string CacheKey = "RolePermissionCache:AllRolePermissions";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _memoryCache;

    public RolePermissionCache(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
    {
        _unitOfWork = unitOfWork;
        _memoryCache = memoryCache;
    }

    public async Task<bool> HasPermissionAsync(UserRole role, PermissionResource resource, PermissionAction action, CancellationToken cancellationToken = default)
    {
        var map = await GetOrLoadAsync(cancellationToken);
        return map.TryGetValue(role, out var permissions) && permissions.Contains(FormatKey(resource, action));
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var map = await GetOrLoadAsync(cancellationToken);
        return map.TryGetValue(role, out var permissions) ? permissions.ToList() : Array.Empty<string>();
    }

    public void Invalidate() => _memoryCache.Remove(CacheKey);

    private async Task<Dictionary<UserRole, HashSet<string>>> GetOrLoadAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(CacheKey, out Dictionary<UserRole, HashSet<string>>? cached) && cached is not null)
        {
            return cached;
        }

        var rolePermissions = await _unitOfWork.Repository<RolePermission>()
            .GetAllAsync(query => query.Include(rp => rp.Permission), cancellationToken);

        var map = new Dictionary<UserRole, HashSet<string>>();
        foreach (var rolePermission in rolePermissions)
        {
            if (rolePermission.Permission is null)
            {
                continue;
            }

            if (!map.TryGetValue(rolePermission.Role, out var permissions))
            {
                permissions = new HashSet<string>();
                map[rolePermission.Role] = permissions;
            }

            permissions.Add(FormatKey(rolePermission.Permission.Resource, rolePermission.Permission.Action));
        }

        _memoryCache.Set(CacheKey, map, CacheTtl);
        return map;
    }

    private static string FormatKey(PermissionResource resource, PermissionAction action) => $"{resource}.{action}";
}
