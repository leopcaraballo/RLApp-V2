using System.Text.Json;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;

namespace RLApp.Adapters.Persistence.Publishers;

public class OutboxEventPublisher : IEventPublisher
{
    private readonly AppDbContext _context;

    public OutboxEventPublisher(AppDbContext context)
    {
        _context = context;
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Type = domainEvent.EventType,
            Payload = JsonSerializer.Serialize((object)domainEvent),
            OccurredAt = domainEvent.OccurredAt
        };

        await _context.OutboxMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishBatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var messages = domainEvents.Select(e => new OutboxMessage
        {
            Type = e.EventType,
            Payload = JsonSerializer.Serialize((object)e),
            OccurredAt = e.OccurredAt
        });

        await _context.OutboxMessages.AddRangeAsync(messages, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
