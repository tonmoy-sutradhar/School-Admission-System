using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Infrastructure.Audit;
using BuildingBlocks.Infrastructure.Auth;
using BuildingBlocks.Infrastructure.Authorization;
using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Infrastructure.Email;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Time;
using BuildingBlocks.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<AuditableInterceptor>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IAuditLogger, NullAuditLogger>();

        if (cacheOptions.UseRedis && !string.IsNullOrWhiteSpace(cacheOptions.RedisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnection;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, DistributedCacheService>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(SystemRoles.Admin, p => p.RequireRole(SystemRoles.Admin));

        foreach (var permission in Permissions.All)
        {
            services.AddAuthorizationBuilder()
                .AddPolicy(permission, policy =>
                    policy.AddRequirements(new PermissionRequirement(permission)));
        }

        return services;
    }
}