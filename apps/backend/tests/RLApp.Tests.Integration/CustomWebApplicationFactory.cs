using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RLApp.Adapters.Persistence.Data;
using Testcontainers.PostgreSql;

namespace RLApp.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string IntegrationJwtSecret = "integration-test-jwt-signing-key-local";

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected virtual Dictionary<string, string?> GetConfigurationOverrides()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = _dbContainer.GetConnectionString(),
            ["Messaging:Enabled"] = "false",
            ["Messaging:Transport"] = "InMemory",
            ["HealthChecks:RabbitMQ:Enabled"] = "false",
            ["Jwt:Secret"] = IntegrationJwtSecret
        };
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var setting in GetConfigurationOverrides())
        {
            if (setting.Value is not null)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(GetConfigurationOverrides());
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove real DbContext and re-add with test container string
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}

public sealed class LocalOutboxWebApplicationFactory : CustomWebApplicationFactory
{
    protected override Dictionary<string, string?> GetConfigurationOverrides()
    {
        var settings = base.GetConfigurationOverrides();
        settings["HealthChecks:RabbitMQ:Enabled"] = "false";
        return settings;
    }
}
