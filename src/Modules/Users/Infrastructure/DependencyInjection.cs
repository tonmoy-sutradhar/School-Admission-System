using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Application;
using Users.Application.Abstractions;
using Users.Infrastructure.Persistence;
using Users.Infrastructure.Services;

namespace Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddUsersApplication();
        services.AddDbContext<UsersDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<AuditableInterceptor>());
        });
        services.AddScoped<IUsersDbContext>(sp => sp.GetRequiredService<UsersDbContext>());
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        return services;
    }
}