using DotNet.Testcontainers.Builders;
using MassTransit;
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
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _dbContainer.GetConnectionString(),
                ["Messaging:Transport"] = "InMemory",
                ["HealthChecks:RabbitMQ:Enabled"] = "false",
                ["Jwt:Secret"] = "ThisIsAVerySecretKeyForTestingPurposeOnly1234567890!"
            });
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

        // Brute force override for health checks and services
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("Messaging__Transport", "InMemory");
        Environment.SetEnvironmentVariable("HealthChecks__RabbitMQ__Enabled", "false");
        Environment.SetEnvironmentVariable("Jwt__Secret", "ThisIsAVerySecretKeyForTestingPurposeOnly1234567890!");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
