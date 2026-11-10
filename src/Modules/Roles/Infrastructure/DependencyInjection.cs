using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roles.Application;
using Roles.Application.Abstractions;
using Roles.Infrastructure.Persistence;
using Roles.Infrastructure.Services;

namespace Roles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRolesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRolesApplication();
        services.AddDbContext<RolesDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<AuditableInterceptor>());
        });
        services.AddScoped<IRolesDbContext>(sp => sp.GetRequiredService<RolesDbContext>());
        services.AddScoped<IRolePermissionLookup, RolePermissionLookup>();
        return services;
    }
}