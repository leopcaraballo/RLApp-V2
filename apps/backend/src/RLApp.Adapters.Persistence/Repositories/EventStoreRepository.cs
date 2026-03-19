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

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var events = new List<DomainEvent>();

        // Map event types to C# domain event classes dynamically
        foreach (var record in records)
        {
            try
            {
                DomainEvent? @event = record.EventType switch
                {
                    "QueueOpened" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    "PatientCheckedIn" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    "PatientLeftWaiting" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    "ConsultantAssigned" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    "PatientCalledForConsultation" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    "ConsultationStarted" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    "ConsultationFinished" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    "PaymentProcessed" => JsonSerializer.Deserialize<dynamic>(record.Payload, options) as DomainEvent,
                    _ => null
                };

                if (@event != null)
                    events.Add(@event);
            }
            catch (JsonException ex)
            {
                // Log and skip malformed events in event stream
                System.Diagnostics.Debug.WriteLine($"Failed to deserialize event {record.Id}: {ex.Message}");
            }
        }

        return events;
    }
}
