using BuildingBlocks.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Roles.Domain.Entities;

namespace Roles.Infrastructure.Persistence;

public static class RolesSeed
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RolesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RolesDbContext>>();

        await db.Database.MigrateAsync(ct);

        foreach (var code in Permissions.All)
        {
            if (await db.Permissions.AnyAsync(p => p.Code == code, ct)) continue;
            db.Permissions.Add(Permission.Create(code, code, $"Permission {code}"));
        }
        await db.SaveChangesAsync(ct);

        await EnsureRoleAsync(db, SystemRoles.Admin, "System administrator", Permissions.All, ct);
        await EnsureRoleAsync(db, SystemRoles.User, "Standard user", [Permissions.UsersView, Permissions.NotificationsView], ct);
        logger.LogInformation("Roles and permissions seeded.");
    }

    private static async Task EnsureRoleAsync(
        RolesDbContext db,
        string name,
        string description,
        IEnumerable<string> permissionCodes,
        CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == name, ct);
        if (role is null)
        {
            role = Role.Create(name, description, isSystem: true);
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
            role = await db.Roles.Include(r => r.RolePermissions).FirstAsync(r => r.Name == name, ct);
        }

        role.ClearPermissions();
        var permissions = await db.Permissions.Where(p => permissionCodes.Contains(p.Code)).ToListAsync(ct);
        foreach (var permission in permissions)
            role.AddPermission(permission);
        await db.SaveChangesAsync(ct);
    }
}