using Microsoft.EntityFrameworkCore;
using Roles.Domain.Entities;

namespace Roles.Application.Abstractions;

public interface IRolesDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}