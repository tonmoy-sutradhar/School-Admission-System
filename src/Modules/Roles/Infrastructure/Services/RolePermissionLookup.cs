using Identity.Application.Abstractions;
using Roles.Application.Services;

namespace Roles.Infrastructure.Services;

public sealed class RolePermissionLookup(RoleService roleService) : IRolePermissionLookup
{
    public Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => roleService.GetUserAccessAsync(userId, cancellationToken);
}