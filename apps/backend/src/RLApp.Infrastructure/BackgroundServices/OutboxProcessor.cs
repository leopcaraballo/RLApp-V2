using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Persistence.Data;

namespace RLApp.Infrastructure.BackgroundServices;

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
                // Resolve the concrete event type from the string name
                // Look in the Domain assembly where events are defined
                var eventType = typeof(RLApp.Domain.Events.WaitingQueueCreated).Assembly
                    .GetTypes()
                    .FirstOrDefault(t => t.Name == message.Type);

                if (eventType == null)
                {
                    _logger.LogWarning("Unknown event type {Type} in outbox message {MessageId}", message.Type, message.Id);
                    message.ProcessedAt = DateTime.UtcNow;
                    continue;
                }

                var eventPayload = JsonSerializer.Deserialize(message.Payload, eventType);
                
                if (eventPayload != null)
                {
                    // Publish to the bus (this will be handled by MassTransit)
                    await publishEndpoint.Publish(eventPayload, eventType, stoppingToken);
                }

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                message.AttemptCount++;
                message.Error = ex.Message;
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}
