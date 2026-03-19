using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Infrastructure.BackgroundServices;
using RLApp.Infrastructure.Security;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Publishers;
using RLApp.Adapters.Persistence.Repositories;
using RLApp.Application.Commands;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

namespace RLApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR registration picking up Application Assembly
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(Command).Assembly);
        });

        // Register security services
        var jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
        services.AddSingleton<IJwtTokenService>(new JwtTokenService(jwtSecret));
        services.AddSingleton<IPasswordHashService, Pbkdf2PasswordHashService>();

        // Add EF Core PostgreSQL DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        // Register adapters for interfaces
        services.AddScoped<IEventStore, EventStoreRepository>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>(); // Pushes to the outbox via EF Core
        services.AddScoped<IProjectionStore, ProjectionStoreRepository>(); // Read model projections

        // Configure MassTransit with RabbitMQ
        services.AddMassTransit(x =>
        {
            // Endpoints and Consumers could be registered here
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                    h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        // Configure the Outbox Background Service
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
