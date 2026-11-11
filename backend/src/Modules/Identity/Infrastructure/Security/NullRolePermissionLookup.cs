using Identity.Application.Abstractions;

namespace Identity.Infrastructure.Security;

public sealed class NullRolePermissionLookup : IRolePermissionLookup
{
    public Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<(IReadOnlyList<string>, IReadOnlyList<string>)>(([], []));
}