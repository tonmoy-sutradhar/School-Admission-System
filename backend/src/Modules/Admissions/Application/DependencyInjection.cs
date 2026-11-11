using Admissions.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Admissions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAdmissionsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ClassService>();
        services.AddScoped<ClassService>();
        services.AddScoped<HoldService>();
        return services;
    }
}