using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace IntegrationTests;

public sealed class AdmissionsWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("school_admission_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "INTEGRATION_TEST_SECRET_KEY_32CHARS_MIN!",
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Cache:UseRedis"] = "false",
                ["EnableHttpsRedirection"] = "false",
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                ["Admissions:HoldDurationSeconds"] = "600",
                ["HOLD_DURATION_SECONDS"] = "600",
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
            });
        });
    }
}
