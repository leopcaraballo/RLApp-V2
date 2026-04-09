using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

/// <summary>
/// Implements the waiting queue repository using PostgreSQL via EF Core.
/// Provides persistence for the WaitingQueue aggregate.
/// Reference: S-003 Queue Open and Check-in, S-002 Consulting Room Lifecycle
/// Implements: ADR-001 (Hexagonal), Event Sourcing with EventStore
/// </summary>
public class WaitingQueueRepository : IWaitingQueueRepository
{
    private readonly AppDbContext _context;
    private readonly IEventStore _eventStore;

    public WaitingQueueRepository(AppDbContext context, IEventStore eventStore)
    {
        _context = context;
        _eventStore = eventStore;
    }

    public async Task<WaitingQueue> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Replay from event store (pure event sourcing)
        var events = await _eventStore.GetEventsByAggregateIdAsync(id, cancellationToken);
        if (events.Count == 0)
            throw new KeyNotFoundException($"Waiting queue {id} not found");

        // Replay events to reconstruct the aggregate
        var queue = ReplayEventsToAggregate(id, events);
        return queue;
    }

    public async Task AddAsync(WaitingQueue waitingQueue, CancellationToken cancellationToken = default)
    {
        // Persist domain events to event store (event sourcing)
        var events = waitingQueue.GetUnraisedEvents();
        await _eventStore.SaveBatchAsync(events, waitingQueue.Version, cancellationToken);
        waitingQueue.SetPersistedVersion(waitingQueue.Version + events.Count);
    }

    public async Task UpdateAsync(WaitingQueue waitingQueue, CancellationToken cancellationToken = default)
    {
        // Persist any new domain events
        var events = waitingQueue.GetUnraisedEvents();
        if (events.Count > 0)
        {
            await _eventStore.SaveBatchAsync(events, waitingQueue.Version, cancellationToken);
            waitingQueue.SetPersistedVersion(waitingQueue.Version + events.Count);
        }
    }

    public async Task<IList<WaitingQueue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder for event-sourced aggregate (would query projection)
        return await Task.FromResult(new List<WaitingQueue>());
    }

    /// <summary>
    /// Replay domain events to reconstruct the aggregate at its current state.
    /// </summary>
    private WaitingQueue ReplayEventsToAggregate(string aggregateId, IList<Domain.Common.DomainEvent> events)
    {
        var createdEvent = events.OfType<WaitingQueueCreated>().FirstOrDefault();
        var queueName = createdEvent?.QueueName ?? aggregateId;

        var queue = (WaitingQueue)Activator.CreateInstance(
            typeof(WaitingQueue),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new object[] { aggregateId, queueName },
            null
        )!;

        typeof(WaitingQueue).GetProperty("IsOpen")?.SetValue(queue, true);

        var patientIds = queue.PatientIds;
        var roomAssignments = queue.PatientRoomAssignments;
        var attentionStates = queue.PatientAttentionStates;

        foreach (var @event in events)
        {
            switch (@event)
            {
                case WaitingQueueCreated created:
                    typeof(WaitingQueue).GetProperty("Name")?.SetValue(queue, created.QueueName);
                    break;
                case PatientCheckedIn checkedIn when !patientIds.Contains(checkedIn.PatientId):
                    patientIds.Add(checkedIn.PatientId);
                    break;
                case PatientCalledAtCashier calledAtCashier:
                    attentionStates[calledAtCashier.PatientId] = "AtCashier";
                    break;
                case PatientPaymentPending paymentPending:
                    attentionStates[paymentPending.PatientId] = "PaymentPending";
                    break;
                case PatientPaymentValidated paymentValidated:
                    attentionStates[paymentValidated.PatientId] = "WaitingForConsultation";
                    break;
                case PatientClaimedForAttention claimed:
                    roomAssignments[claimed.PatientId] = claimed.RoomId;
                    attentionStates[claimed.PatientId] = claimed.RepresentsStartedAttention
                        ? "InConsultation"
                        : "Claimed";
                    break;
                case PatientCalled called:
                    roomAssignments[called.PatientId] = called.RoomId;
                    attentionStates[called.PatientId] = "Called";
                    break;
                case PatientAttentionCompleted completed:
                    patientIds.Remove(completed.PatientId);
                    roomAssignments.Remove(completed.PatientId);
                    attentionStates.Remove(completed.PatientId);
                    break;
                case PatientAbsentAtConsultation absentConsultation:
                    patientIds.Remove(absentConsultation.PatientId);
                    roomAssignments.Remove(absentConsultation.PatientId);
                    attentionStates.Remove(absentConsultation.PatientId);
                    break;
                case PatientAbsentAtCashier absentCashier:
                    patientIds.Remove(absentCashier.PatientId);
                    roomAssignments.Remove(absentCashier.PatientId);
                    attentionStates.Remove(absentCashier.PatientId);
                    break;
            }
        }

        queue.SetPersistedVersion(events.Count);
        queue.ClearUnraisedEvents();
        return queue;
    }
}
