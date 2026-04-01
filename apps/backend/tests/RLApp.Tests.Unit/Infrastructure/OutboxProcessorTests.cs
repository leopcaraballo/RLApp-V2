namespace RLApp.Tests.Unit.Infrastructure;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Adapters.Persistence.Persistence;
using RLApp.Domain.Events;
using RLApp.Infrastructure.BackgroundServices;

public class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingMessagesAsync_RespectsConfiguredBatchSizeAndMarksProcessedMessages()
    {
        var dispatcher = Substitute.For<IOutboxMessageDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await using var provider = BuildServiceProvider("outbox-success", dispatcher);
        await SeedOutboxMessagesAsync(provider, 2);

        var processor = new OutboxProcessor(
            provider,
            NullLogger<OutboxProcessor>.Instance,
            Options.Create(new OutboxProcessorOptions { PollingIntervalMs = 500, BatchSize = 1 }),
            Substitute.For<IOutboxProcessingSignal>());

        var processedMessages = await processor.ProcessPendingMessagesAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messages = await context.OutboxMessages
            .OrderBy(m => m.OccurredAt)
            .ToListAsync();

        Assert.Equal(1, processedMessages);
        Assert.NotNull(messages[0].ProcessedAt);
        Assert.Null(messages[1].ProcessedAt);
        Assert.Equal(0, messages[0].AttemptCount);

        await dispatcher.Received(1)
            .DispatchAsync(Arg.Any<object>(), Arg.Is<Type>(type => type == typeof(WaitingQueueCreated)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenPublishFails_IncrementsAttemptCountAndStoresError()
    {
        var dispatcher = Substitute.For<IOutboxMessageDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("publish-failed"));

        await using var provider = BuildServiceProvider("outbox-failure", dispatcher);
        await SeedOutboxMessagesAsync(provider, 1);

        var processor = new OutboxProcessor(
            provider,
            NullLogger<OutboxProcessor>.Instance,
            Options.Create(new OutboxProcessorOptions { PollingIntervalMs = 500, BatchSize = 10 }),
            Substitute.For<IOutboxProcessingSignal>());

        var processedMessages = await processor.ProcessPendingMessagesAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await context.OutboxMessages.SingleAsync();

        Assert.Equal(0, processedMessages);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal("publish-failed", message.Error);
    }

    private static ServiceProvider BuildServiceProvider(string databaseName, IOutboxMessageDispatcher dispatcher)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped(_ => dispatcher);

        return services.BuildServiceProvider();
    }

    private static async Task SeedOutboxMessagesAsync(ServiceProvider provider, int count)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baseTime = DateTime.UtcNow.AddSeconds(-2);
        for (var index = 0; index < count; index++)
        {
            var domainEvent = new WaitingQueueCreated(
                $"queue-{index + 1}",
                $"Queue {index + 1}",
                $"corr-{index + 1}");

            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = domainEvent.AggregateId,
                CorrelationId = domainEvent.CorrelationId,
                Type = nameof(WaitingQueueCreated),
                Payload = JsonSerializer.Serialize(domainEvent),
                OccurredAt = baseTime.AddMilliseconds(index)
            });
        }

        await context.SaveChangesAsync();
    }
}
