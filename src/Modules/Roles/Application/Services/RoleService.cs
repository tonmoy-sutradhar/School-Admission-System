using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Roles.Application.Abstractions;
using Roles.Application.DTOs;
using Roles.Domain.Entities;

namespace Roles.Application.Services;

public sealed class RoleService(
    IRolesDbContext db,
    IAuditLogger auditLogger,
    ICacheService cache)
{
    public async Task<Result<IReadOnlyList<RoleDto>>> GetRolesAsync(CancellationToken ct = default)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<RoleDto>>(roles.Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<PermissionDto>>> GetPermissionsAsync(CancellationToken ct = default)
    {
        var permissions = await db.Permissions.AsNoTracking().OrderBy(p => p.Code).ToListAsync(ct);
        return Result.Success<IReadOnlyList<PermissionDto>>(
            permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Description)).ToList());
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        if (await db.Roles.AnyAsync(r => r.Name == request.Name, ct))
            return Result.Failure<RoleDto>(Error.Conflict("Role already exists."));

        var role = Role.Create(request.Name, request.Description);
        await ApplyPermissionsAsync(role, request.PermissionCodes, ct);
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        await auditLogger.LogAsync("RoleCreated", nameof(Role), role.Id.ToString(), cancellationToken: ct);
        return Result.Success(await GetRoleDtoAsync(role.Id, ct));
    }

    public async Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null)
            return Result.Failure<RoleDto>(Error.NotFound("Role not found."));

        try { role.Update(request.Name, request.Description); }
        catch (InvalidOperationException ex) { return Result.Failure<RoleDto>(Error.Validation(ex.Message)); }

        role.ClearPermissions();
        await ApplyPermissionsAsync(role, request.PermissionCodes, ct);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPrefixAsync("roles:", ct);
        await auditLogger.LogAsync("RoleUpdated", nameof(Role), role.Id.ToString(), cancellationToken: ct);
        return Result.Success(await GetRoleDtoAsync(role.Id, ct));
    }

    public async Task<Result> AssignRolesAsync(Guid userId, AssignRolesRequest request, CancellationToken ct = default)
    {
        var roles = await db.Roles.Where(r => request.RoleNames.Contains(r.Name)).ToListAsync(ct);
        if (roles.Count != request.RoleNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            return Result.Failure(Error.Validation("One or more roles were not found."));

        var existing = await db.UserRoles.Where(x => x.UserId == userId).ToListAsync(ct);
        db.UserRoles.RemoveRange(existing);
        foreach (var role in roles)
            db.UserRoles.Add(UserRole.Create(userId, role.Id));

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync($"roles:user:{userId}", ct);
        await auditLogger.LogAsync("RolesAssigned", "UserRole", userId.ToString(), string.Join(",", request.RoleNames), ct);
        return Result.Success();
    }

    public async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetUserAccessAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"roles:user:{userId}";
        var cached = await cache.GetAsync<UserAccessCache>(cacheKey, ct);
        if (cached is not null) return (cached.Roles, cached.Permissions);

        var roleIds = await db.UserRoles.Where(x => x.UserId == userId).Select(x => x.RoleId).ToListAsync(ct);
        var roles = await db.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToListAsync(ct);
        var permissions = await db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(ct);

        await cache.SetAsync(cacheKey, new UserAccessCache(roles, permissions), TimeSpan.FromMinutes(15), ct);
        return (roles, permissions);
    }

    private async Task ApplyPermissionsAsync(Role role, IReadOnlyList<string>? codes, CancellationToken ct)
    {
        if (codes is null || codes.Count == 0) return;
        var permissions = await db.Permissions.Where(p => codes.Contains(p.Code)).ToListAsync(ct);
        foreach (var permission in permissions)
            role.AddPermission(permission);
    }

    private async Task<RoleDto> GetRoleDtoAsync(Guid id, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstAsync(r => r.Id == id, ct);
        return Map(role);
    }

    private static RoleDto Map(Role role) =>
        new(role.Id, role.Name, role.Description, role.IsSystem,
            role.RolePermissions.Select(rp => rp.Permission!.Code).ToList());

    private sealed record UserAccessCache(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
}