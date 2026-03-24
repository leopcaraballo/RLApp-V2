using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Infrastructure.BackgroundServices;
using RLApp.Infrastructure.Data;
using RLApp.Infrastructure.Security;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Publishers;
using RLApp.Adapters.Persistence.Persistence;
using RLApp.Adapters.Persistence.Repositories;
using RLApp.Application.Commands;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;
using Polly;
using Polly.Retry;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using System;

namespace RLApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, Action<IBusRegistrationConfigurator>? extraMassTransitConfig = null)
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
        services.AddScoped<IPersistenceSession, EfPersistenceSession>();

        // Register aggregate repositories
        services.AddScoped<IConsultingRoomRepository, ConsultingRoomRepository>();
        services.AddScoped<IWaitingQueueRepository, WaitingQueueRepository>();
        services.AddScoped<IStaffUserRepository, StaffUserRepository>();
        services.AddScoped<IAuditStore, AuditStoreRepository>();

        // Register database seeding service
        services.AddScoped<RLApp.Infrastructure.Data.DbSeeder>();

        // Configure MassTransit with RabbitMQ
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitPort = ushort.TryParse(configuration["RabbitMQ:Port"], out var parsedRabbitPort)
            ? parsedRabbitPort
            : (ushort)5672;
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";
        var messagingEnabled = bool.TryParse(configuration["Messaging:Enabled"], out var messagingEnabledValue)
            ? messagingEnabledValue
            : true;
        var messagingTransport = configuration["Messaging:Transport"] ?? "RabbitMQ";
        var useInMemoryTransport = string.Equals(messagingTransport, "InMemory", StringComparison.OrdinalIgnoreCase);
        var enableRabbitHealthCheck = bool.TryParse(configuration["HealthChecks:RabbitMQ:Enabled"], out var rabbitHealthCheckEnabled)
            ? rabbitHealthCheckEnabled
            : !useInMemoryTransport;

        if (messagingEnabled)
        {
            services.AddMassTransit(x =>
            {
                // Register Core Consumers
                x.AddConsumer<RLApp.Adapters.Messaging.Consumers.WaitingRoomMonitorConsumer>();
                x.AddConsumer<RLApp.Adapters.Messaging.Consumers.DashboardConsumer>();
                x.AddConsumer<RLApp.Adapters.Messaging.Consumers.QueueStateConsumer>();

                // Register Sagas
                x.AddSagaStateMachine<RLApp.Adapters.Messaging.Sagas.ConsultationSaga, RLApp.Adapters.Messaging.Sagas.ConsultationState>()
                    .InMemoryRepository();

                // Apply extra config (e.g. from API layer)
                extraMassTransitConfig?.Invoke(x);

                if (useInMemoryTransport)
                {
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                    });
                }
                else
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(rabbitHost, rabbitPort, "/", h =>
                        {
                            h.Username(rabbitUser);
                            h.Password(rabbitPass);
                        });

                        cfg.ConfigureEndpoints(context);
                    });
                }
            });
        }

        // Add SignalR
        services.AddSignalR();

        // Add Health Checks
        var rabbitConnectionString = $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:{rabbitPort}/";

        var healthChecks = services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Postgres") ?? "", tags: ["ready"])
            .AddCheck<RLApp.Infrastructure.HealthChecks.ProjectionLagHealthCheck>("ProjectionLag", tags: ["ready"])
            .AddCheck("Self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        if (messagingEnabled && enableRabbitHealthCheck)
        {
            healthChecks.AddRabbitMQ(async sp =>
            {
                var factory = new ConnectionFactory { Uri = new Uri(rabbitConnectionString) };
                return await factory.CreateConnectionAsync();
            }, name: "RabbitMQ", tags: ["ready"]);
        }

        // Configure Polly Resilience Pipelines (using Microsoft.Extensions.Resilience)
        services.AddResiliencePipeline("default", builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1)
            });
        });

        // Configure the Outbox Background Service
        if (messagingEnabled)
        {
            services.AddHostedService<OutboxProcessor>();
        }

        return services;
    }
}
