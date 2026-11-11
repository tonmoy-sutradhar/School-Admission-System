using AuditLogs.Application;
using AuditLogs.Application.Abstractions;
using AuditLogs.Infrastructure.Persistence;
using AuditLogs.Infrastructure.Services;
using BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLogs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditLogsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuditLogsApplication();
        services.AddDbContext<AuditLogsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IAuditLogsDbContext>(sp => sp.GetRequiredService<AuditLogsDbContext>());
        services.AddScoped<IAuditLogger, AuditLogger>();
        return services;
    }
}