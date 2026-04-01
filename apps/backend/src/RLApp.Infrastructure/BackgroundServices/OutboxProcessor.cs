using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Adapters.Persistence.Persistence;

namespace RLApp.Infrastructure.BackgroundServices;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxProcessorOptions _options;
    private readonly IOutboxProcessingSignal _outboxProcessingSignal;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxProcessorOptions> options,
        IOutboxProcessingSignal outboxProcessingSignal)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _outboxProcessingSignal = outboxProcessingSignal;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox Processor is starting with polling interval {PollingIntervalMs} ms and batch size {BatchSize}.",
            _options.PollingIntervalMs,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedMessages = await ProcessPendingMessagesAsync(stoppingToken);
                if (processedMessages > 0)
                {
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages.");
            }

            await _outboxProcessingSignal.WaitAsync(_options.PollingInterval, stoppingToken);
        }
    }

    internal async Task<int> ProcessPendingMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messageDispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();

        var pendingCount = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .CountAsync(stoppingToken);

        OutboxProcessorTelemetry.RecordBacklog(pendingCount);

        if (pendingCount == 0)
            return 0;

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(_options.BatchSize)
            .ToListAsync(stoppingToken);

        var publishedCount = 0;
        var failedCount = 0;
        var deadLetterCount = 0;
        var startedAt = DateTime.UtcNow;

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
                    MoveToDeadLetter(context, message, $"Unknown event type {message.Type}");
                    deadLetterCount++;
                    OutboxProcessorTelemetry.RecordDeadLetter(message.Type);
                    continue;
                }

                object? eventPayload;

                try
                {
                    eventPayload = JsonSerializer.Deserialize(message.Payload, eventType);
                }
                catch (JsonException ex)
                {
                    MoveToDeadLetter(context, message, $"Invalid payload for event type {message.Type}: {ex.Message}");
                    deadLetterCount++;
                    OutboxProcessorTelemetry.RecordDeadLetter(message.Type);
                    continue;
                }

                if (eventPayload == null)
                {
                    MoveToDeadLetter(context, message, $"Invalid payload for event type {message.Type}: deserialized payload was null.");
                    deadLetterCount++;
                    OutboxProcessorTelemetry.RecordDeadLetter(message.Type);
                    continue;
                }

                var publishStartedAt = DateTime.UtcNow;

                await messageDispatcher.DispatchAsync(eventPayload, eventType, stoppingToken);

                var processedAt = DateTime.UtcNow;
                message.ProcessedAt = processedAt;
                message.Error = null;
                publishedCount++;

                OutboxProcessorTelemetry.RecordPublished(
                    message.Type,
                    processedAt - publishStartedAt,
                    processedAt - message.OccurredAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch outbox message {MessageId}", message.Id);
                message.AttemptCount++;
                message.Error = ex.Message;
                failedCount++;
                OutboxProcessorTelemetry.RecordFailed(message.Type);
            }
        }

        await context.SaveChangesAsync(stoppingToken);

        _logger.LogInformation(
            "Outbox batch processed. Pending {PendingCount}, Processed {ProcessedCount}, Published {PublishedCount}, Failed {FailedCount}, DeadLetter {DeadLetterCount}, DurationMs {DurationMs}.",
            pendingCount,
            messages.Count,
            publishedCount,
            failedCount,
            deadLetterCount,
            (DateTime.UtcNow - startedAt).TotalMilliseconds);

        return publishedCount + deadLetterCount;
    }

    private void MoveToDeadLetter(AppDbContext context, OutboxMessage message, string failureReason)
    {
        _logger.LogWarning(
            "Moving outbox message {MessageId} with type {EventType} to dead-letter storage. Reason: {FailureReason}",
            message.Id,
            message.Type,
            failureReason);

        context.OutboxDeadLetterMessages.Add(new OutboxDeadLetterMessage
        {
            Id = message.Id,
            AggregateId = message.AggregateId,
            CorrelationId = message.CorrelationId,
            Type = message.Type,
            Payload = message.Payload,
            OccurredAt = message.OccurredAt,
            FailedAt = DateTime.UtcNow,
            FailureReason = failureReason
        });

        context.OutboxMessages.Remove(message);
    }
}
