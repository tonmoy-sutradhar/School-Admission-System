using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Services;

namespace Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<NotificationService>();
        return services;
    }
}