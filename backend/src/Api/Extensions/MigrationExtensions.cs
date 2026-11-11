using Admissions.Infrastructure.Persistence;
using AuditLogs.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Persistence;
using Roles.Infrastructure.Persistence;
using Users.Infrastructure.Persistence;

namespace Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyModuleMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        await MigrateAsync<IdentityDbContext>(sp);
        await MigrateAsync<UsersDbContext>(sp);
        await RolesSeed.SeedAsync(app.Services);
        await MigrateAsync<NotificationsDbContext>(sp);
        await MigrateAsync<AuditLogsDbContext>(sp);
        await MigrateAsync<AdmissionsDbContext>(sp);
    }

    private static async Task MigrateAsync<TContext>(IServiceProvider sp) where TContext : DbContext
    {
        var db = sp.GetRequiredService<TContext>();
        await db.Database.MigrateAsync();
    }
}