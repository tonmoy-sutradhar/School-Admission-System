using AuditLogs.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLogs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditLogsApplication(this IServiceCollection services)
    {
        services.AddScoped<AuditLogService>();
        return services;
    }
}