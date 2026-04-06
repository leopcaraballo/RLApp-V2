namespace RLApp.Tests.Unit.Infrastructure;

using Microsoft.EntityFrameworkCore;
using NSubstitute;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Adapters.Persistence.Persistence;

public class EfPersistenceSessionTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenOutboxMessagesAreAdded_NotifiesProcessorAfterPersisting()
    {
        var databaseName = $"ef-session-outbox-{Guid.NewGuid()}";
        var signal = Substitute.For<IOutboxProcessingSignal>();

        await using (var context = CreateContext(databaseName))
        {
            var session = new EfPersistenceSession(context, signal);

            context.OutboxMessages.Add(new OutboxMessage
            {
                AggregateId = "queue-1",
                CorrelationId = "corr-1",
                Type = "WaitingQueueCreated",
                Payload = "{}",
                OccurredAt = DateTime.UtcNow
            });

            await session.SaveChangesAsync();
        }

        await using (var verificationContext = CreateContext(databaseName))
        {
            Assert.Equal(1, await verificationContext.OutboxMessages.CountAsync());
        }

        signal.Received(1).NotifyNewMessages();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoOutboxMessagesAreAdded_DoesNotNotifyProcessor()
    {
        var signal = Substitute.For<IOutboxProcessingSignal>();
        await using var context = CreateContext($"ef-session-no-outbox-{Guid.NewGuid()}");
        var session = new EfPersistenceSession(context, signal);

        await session.SaveChangesAsync();

        signal.DidNotReceive().NotifyNewMessages();
    }

    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }
}
