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
            AggregateId = domainEvent.AggregateId,
            CorrelationId = domainEvent.CorrelationId,
            Type = domainEvent.EventType,
            Payload = JsonSerializer.Serialize((object)domainEvent),
            OccurredAt = domainEvent.OccurredAt
        };

        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task PublishBatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var messages = domainEvents.Select(e => new OutboxMessage
        {
            AggregateId = e.AggregateId,
            CorrelationId = e.CorrelationId,
            Type = e.EventType,
            Payload = JsonSerializer.Serialize((object)e),
            OccurredAt = e.OccurredAt
        });

        await _context.OutboxMessages.AddRangeAsync(messages, cancellationToken);
    }
}
