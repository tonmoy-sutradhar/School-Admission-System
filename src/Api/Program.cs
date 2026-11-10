using Api.Extensions;
using Api.Middleware;
using AspNetCoreRateLimit;
using AuditLogs.Infrastructure;
using BuildingBlocks.Infrastructure;
using Identity.Infrastructure;
using Identity.Presentation.Controllers;
using Microsoft.Extensions.Hosting;
using Notifications.Infrastructure;
using Roles.Infrastructure;
using Serilog;
using Users.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddBuildingBlocks(builder.Configuration);
    builder.Services.AddIdentityModule(builder.Configuration);
    builder.Services.AddRolesModule(builder.Configuration);
    builder.Services.AddUsersModule(builder.Configuration);
    builder.Services.AddNotificationsModule(builder.Configuration);
    builder.Services.AddAuditLogsModule(builder.Configuration);

    builder.Services.AddControllers()
        .AddApplicationPart(typeof(AuthController).Assembly)
        .AddApplicationPart(typeof(Users.Presentation.Controllers.UsersController).Assembly)
        .AddApplicationPart(typeof(Roles.Presentation.Controllers.RolesController).Assembly)
        .AddApplicationPart(typeof(Notifications.Presentation.Controllers.NotificationsController).Assembly)
        .AddApplicationPart(typeof(AuditLogs.Presentation.Controllers.AuditLogsController).Assembly);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "Modular Monolith API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Default", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"];
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        });
    });

    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    var healthChecks = builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

    if (builder.Configuration.GetValue("Cache:UseRedis", false))
    {
        healthChecks.AddRedis(
            builder.Configuration.GetSection("Cache:RedisConnection").Value ?? "localhost:6379",
            name: "redis");
    }

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("Default");
    app.UseIpRateLimiting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        await app.ApplyModuleMigrationsAsync();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;