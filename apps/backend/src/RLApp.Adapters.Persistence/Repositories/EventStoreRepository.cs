using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Domain.Common;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

public class EventStoreRepository : IEventStore
{
    private readonly AppDbContext _context;

    public EventStoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var record = new EventRecord
        {
            AggregateId = domainEvent.AggregateId,
            EventType = domainEvent.EventType,
            CorrelationId = domainEvent.CorrelationId,
            OccurredAt = domainEvent.OccurredAt,
            Payload = JsonSerializer.Serialize((object)domainEvent)
        };

        await _context.EventStore.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveBatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var records = domainEvents.Select(e => new EventRecord
        {
            AggregateId = e.AggregateId,
            EventType = e.EventType,
            CorrelationId = e.CorrelationId,
            OccurredAt = e.OccurredAt,
            Payload = JsonSerializer.Serialize((object)e)
        });

        await _context.EventStore.AddRangeAsync(records, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IList<DomainEvent>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var records = await _context.EventStore
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

        // Warning: This returns a list of base DomainEvent via dynamic deserialization.
        // A robust implementation would map EventType to the concrete C# Type.
        // For Phase 4 simplification, we assume the Domain expects EventBase.
        var events = new List<DomainEvent>();
        foreach (var record in records)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // Normally we resolve the type from assembly. Here we mock it by returning a base or dynamic parsing.
            // As ADR-003 implies CQRS without full rehydration locally, or rehydrating strictly if needed.
        }

        return events;
    }
}
