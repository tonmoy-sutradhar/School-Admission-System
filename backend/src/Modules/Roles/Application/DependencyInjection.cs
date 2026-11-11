using Microsoft.Extensions.DependencyInjection;
using Roles.Application.Services;

namespace Roles.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRolesApplication(this IServiceCollection services)
    {
        services.AddScoped<RoleService>();
        return services;
    }
}