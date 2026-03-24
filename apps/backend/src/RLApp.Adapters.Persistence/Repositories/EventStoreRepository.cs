using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

public class EventStoreRepository : IEventStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<string, Type> EventTypeMap = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(WaitingQueueCreated)] = typeof(WaitingQueueCreated),
        [nameof(PatientCheckedIn)] = typeof(PatientCheckedIn),
        [nameof(PatientCalledAtCashier)] = typeof(PatientCalledAtCashier),
        [nameof(PatientPaymentValidated)] = typeof(PatientPaymentValidated),
        [nameof(PatientPaymentPending)] = typeof(PatientPaymentPending),
        [nameof(PatientAbsentAtCashier)] = typeof(PatientAbsentAtCashier),
        [nameof(PatientCancelledByPayment)] = typeof(PatientCancelledByPayment),
        [nameof(ConsultingRoomActivated)] = typeof(ConsultingRoomActivated),
        [nameof(ConsultingRoomDeactivated)] = typeof(ConsultingRoomDeactivated),
        [nameof(PatientClaimedForAttention)] = typeof(PatientClaimedForAttention),
        [nameof(PatientCalled)] = typeof(PatientCalled),
        [nameof(PatientAttentionCompleted)] = typeof(PatientAttentionCompleted),
        [nameof(PatientAbsentAtConsultation)] = typeof(PatientAbsentAtConsultation),
        [nameof(PatientCancelledByAbsence)] = typeof(PatientCancelledByAbsence)
    };

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
    }

    public async Task<IList<DomainEvent>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var records = await _context.EventStore
            .AsNoTracking()
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

        var events = new List<DomainEvent>();

        foreach (var record in records)
        {
            var @event = DeserializeEvent(record);
            if (@event != null)
                events.Add(@event);
        }

        return events;
    }

    public async Task<IList<DomainEvent>> GetEventsByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var records = await _context.EventStore
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

        var events = new List<DomainEvent>();

        foreach (var record in records)
        {
            var @event = DeserializeEvent(record);
            if (@event != null)
                events.Add(@event);
        }

        return events;
    }

    private static DomainEvent? DeserializeEvent(EventRecord record)
    {
        if (!EventTypeMap.TryGetValue(record.EventType, out var eventClrType))
            return null;

        try
        {
            return JsonSerializer.Deserialize(record.Payload, eventClrType, SerializerOptions) as DomainEvent;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to deserialize event {record.Id}: {ex.Message}");
            return null;
        }
    }
}
