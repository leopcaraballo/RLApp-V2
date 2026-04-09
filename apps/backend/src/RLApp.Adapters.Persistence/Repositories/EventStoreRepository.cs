using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        [nameof(ConsultingRoomActivated)] = typeof(ConsultingRoomActivated),
        [nameof(ConsultingRoomDeactivated)] = typeof(ConsultingRoomDeactivated),
        [nameof(PatientClaimedForAttention)] = typeof(PatientClaimedForAttention),
        [nameof(PatientCalled)] = typeof(PatientCalled),
        [nameof(PatientAttentionCompleted)] = typeof(PatientAttentionCompleted),
        [nameof(PatientAbsentAtConsultation)] = typeof(PatientAbsentAtConsultation),
        [nameof(PatientTrajectoryOpened)] = typeof(PatientTrajectoryOpened),
        [nameof(PatientTrajectoryStageRecorded)] = typeof(PatientTrajectoryStageRecorded),
        [nameof(PatientTrajectoryCompleted)] = typeof(PatientTrajectoryCompleted),
        [nameof(PatientTrajectoryCancelled)] = typeof(PatientTrajectoryCancelled),
        [nameof(PatientTrajectoryRebuilt)] = typeof(PatientTrajectoryRebuilt)
    };

    private readonly AppDbContext _context;
    private readonly ILogger<EventStoreRepository> _logger;

    public EventStoreRepository(AppDbContext context, ILogger<EventStoreRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task SaveAsync(DomainEvent domainEvent, int? expectedVersion = null, CancellationToken cancellationToken = default)
        => SaveBatchAsync(new[] { domainEvent }, expectedVersion, cancellationToken);

    public async Task SaveBatchAsync(IEnumerable<DomainEvent> domainEvents, int? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var eventList = domainEvents.ToList();
        if (eventList.Count == 0)
        {
            return;
        }

        List<EventRecord> records;

        if (expectedVersion is not null)
        {
            var aggregateIds = eventList
                .Select(@event => @event.AggregateId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (aggregateIds.Count != 1)
            {
                throw new DomainException("Expected version can only be used when persisting a single aggregate stream");
            }

            var aggregateId = aggregateIds[0];
            var currentVersion = await GetCurrentVersionAsync(aggregateId, cancellationToken);

            if (currentVersion != expectedVersion.Value)
            {
                throw DomainException.ConcurrencyConflict(aggregateId, expectedVersion.Value, currentVersion);
            }

            records = BuildRecords(eventList, currentVersion);
        }
        else
        {
            records = new List<EventRecord>(eventList.Count);

            foreach (var group in eventList.GroupBy(@event => @event.AggregateId, StringComparer.Ordinal))
            {
                var currentVersion = await GetCurrentVersionAsync(group.Key, cancellationToken);
                records.AddRange(BuildRecords(group, currentVersion));
            }
        }

        await _context.EventStore.AddRangeAsync(records, cancellationToken);
    }

    public async Task<IList<DomainEvent>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var records = await _context.EventStore
            .AsNoTracking()
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.SequenceNumber)
            .ThenBy(e => e.OccurredAt)
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
            .ThenBy(e => e.Id)
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

    public async Task<IList<DomainEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await _context.EventStore
            .AsNoTracking()
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        var events = new List<DomainEvent>();

        foreach (var record in records)
        {
            var @event = DeserializeEvent(record);
            if (@event != null)
            {
                events.Add(@event);
            }
        }

        return events;
    }

    private DomainEvent? DeserializeEvent(EventRecord record)
    {
        if (!EventTypeMap.TryGetValue(record.EventType, out var eventClrType))
            return null;

        try
        {
            return JsonSerializer.Deserialize(record.Payload, eventClrType, SerializerOptions) as DomainEvent;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize event {EventId}", record.Id);
            return null;
        }
    }

    private async Task<int> GetCurrentVersionAsync(string aggregateId, CancellationToken cancellationToken)
    {
        var persistedVersion = await _context.EventStore
            .AsNoTracking()
            .Where(record => record.AggregateId == aggregateId)
            .Select(record => (int?)record.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var pendingVersion = _context.EventStore.Local
            .Where(record => string.Equals(record.AggregateId, aggregateId, StringComparison.Ordinal))
            .Select(record => record.SequenceNumber)
            .DefaultIfEmpty(0)
            .Max();

        return Math.Max(persistedVersion, pendingVersion);
    }

    private static List<EventRecord> BuildRecords(IEnumerable<DomainEvent> domainEvents, int currentVersion)
    {
        var nextSequenceNumber = currentVersion + 1;

        return domainEvents.Select(@event => new EventRecord
        {
            AggregateId = @event.AggregateId,
            SequenceNumber = nextSequenceNumber++,
            EventType = @event.EventType,
            CorrelationId = @event.CorrelationId,
            OccurredAt = @event.OccurredAt,
            Payload = JsonSerializer.Serialize((object)@event)
        }).ToList();
    }
}
