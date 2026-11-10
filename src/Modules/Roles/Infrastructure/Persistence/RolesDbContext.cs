using Microsoft.EntityFrameworkCore;
using Roles.Application.Abstractions;
using Roles.Domain.Entities;

namespace Roles.Infrastructure.Persistence;

public sealed class RolesDbContext(DbContextOptions<RolesDbContext> options)
    : DbContext(options), IRolesDbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("roles");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RolesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}