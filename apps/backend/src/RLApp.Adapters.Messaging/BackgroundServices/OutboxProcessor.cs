using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Persistence.Data;

namespace RLApp.Adapters.Messaging.BackgroundServices;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(50)
            .ToListAsync(stoppingToken);

        if (!messages.Any())
            return;

        foreach (var message in messages)
        {
            try
            {
                // In a production scenario, we'd deserialize to the actual event type using reflection or a registry.
                // For Phase 4 we use dynamic/object publishing to demonstrate the pipeline over MassTransit.
                var eventPayload = JsonSerializer.Deserialize<dynamic>(message.Payload);
                
                if (eventPayload != null)
                {
                    // MassTransit handles generic object publishing if configured, 
                    // or ideally we'd use IPublishEndpoint.Publish(object, Type)
                    await publishEndpoint.Publish(eventPayload, stoppingToken);
                }

                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                message.Error = ex.Message;
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}
