using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Services;

namespace Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<UserService>();
        services.AddScoped<UserService>();
        return services;
    }
}