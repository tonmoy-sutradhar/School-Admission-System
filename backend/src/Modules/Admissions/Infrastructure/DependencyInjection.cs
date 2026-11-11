using Admissions.Application;
using Admissions.Application.Abstractions;
using Admissions.Application.Options;
using Admissions.Infrastructure.Background;
using Admissions.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Admissions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdmissionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AdmissionsOptions>()
            .Bind(configuration.GetSection(AdmissionsOptions.SectionName))
            .PostConfigure(options =>
            {
                var envSeconds = configuration["HOLD_DURATION_SECONDS"];
                if (int.TryParse(envSeconds, out var seconds) && seconds > 0)
                    options.HoldDurationSeconds = seconds;
            });

        services.AddAdmissionsApplication();
        services.AddDbContext<AdmissionsDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<AuditableInterceptor>());
        });
        services.AddScoped<IAdmissionsDbContext>(sp => sp.GetRequiredService<AdmissionsDbContext>());
        services.AddHostedService<HoldExpirationWorker>();
        return services;
    }
}
