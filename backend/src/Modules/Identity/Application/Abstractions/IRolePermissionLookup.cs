namespace Identity.Application.Abstractions;

public interface IRolePermissionLookup
{
    Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}