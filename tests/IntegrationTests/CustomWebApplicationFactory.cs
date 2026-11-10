using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "INTEGRATION_TEST_SECRET_KEY_32CHARS_MIN!",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Cache:UseRedis"] = "false",
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=modular_monolith_test;Username=postgres;Password=postgres"
            });
        });
    }
}