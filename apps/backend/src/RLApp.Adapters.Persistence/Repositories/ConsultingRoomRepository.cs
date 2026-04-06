using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

/// <summary>
/// Implements the consulting room repository using PostgreSQL via EF Core.
/// Provides persistence for the ConsultingRoom aggregate.
/// Reference: S-002 Consulting Room Lifecycle, UC-003, UC-005, UC-006
/// Implements: ADR-001 (Hexagonal), Event Sourcing with EventStore
/// </summary>
public class ConsultingRoomRepository : IConsultingRoomRepository
{
    private readonly AppDbContext _context;
    private readonly IEventStore _eventStore;

    public ConsultingRoomRepository(AppDbContext context, IEventStore eventStore)
    {
        _context = context;
        _eventStore = eventStore;
    }

    public async Task<ConsultingRoom> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Replay from event store (pure event sourcing)
        var events = await _eventStore.GetEventsByAggregateIdAsync(id, cancellationToken);
        if (events.Count == 0)
            throw new KeyNotFoundException($"Consulting room {id} not found");

        // Replay events to reconstruct the aggregate
        var room = ReplayEventsToAggregate(id, events);
        return room;
    }

    public async Task AddAsync(ConsultingRoom consultingRoom, CancellationToken cancellationToken = default)
    {
        // Persist domain events to event store (event sourcing)
        var events = consultingRoom.GetUnraisedEvents();
        await _eventStore.SaveBatchAsync(events, consultingRoom.Version, cancellationToken);
        consultingRoom.SetPersistedVersion(consultingRoom.Version + events.Count);
    }

    public async Task UpdateAsync(ConsultingRoom consultingRoom, CancellationToken cancellationToken = default)
    {
        // Persist any new domain events (event sourcing)
        var events = consultingRoom.GetUnraisedEvents();
        if (events.Count > 0)
        {
            await _eventStore.SaveBatchAsync(events, consultingRoom.Version, cancellationToken);
            consultingRoom.SetPersistedVersion(consultingRoom.Version + events.Count);
        }
    }

    public async Task<IList<ConsultingRoom>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Note: For a real implementation, we'd query a snapshot table or projection
        // For now, this is a placeholder
        return await Task.FromResult(new List<ConsultingRoom>());
    }

    public async Task<IList<ConsultingRoom>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        // Note: For a real implementation, we'd query a projection
        // For now, this is a placeholder
        return await Task.FromResult(new List<ConsultingRoom>());
    }

    /// <summary>
    /// Replay domain events to reconstruct the aggregate at its current state.
    /// Note: In a full event sourcing implementation, we would deserialize each event
    /// and apply it to the aggregate to reconstruct its exact state.
    /// For now, this is a simplified version that creates a base instance.
    /// </summary>
    private ConsultingRoom ReplayEventsToAggregate(string aggregateId, IList<Domain.Common.DomainEvent> events)
    {
        var activatedEvent = events.OfType<ConsultingRoomActivated>().FirstOrDefault();
        var roomName = activatedEvent?.RoomName ?? aggregateId;

        // Create aggregate via reflection to access private constructor
        var room = (ConsultingRoom)Activator.CreateInstance(
            typeof(ConsultingRoom),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new object[] { aggregateId, roomName },
            null
        )!;

        var roomType = typeof(ConsultingRoom);

        foreach (var @event in events)
        {
            switch (@event)
            {
                case ConsultingRoomActivated activated:
                    roomType.GetProperty("RoomName")?.SetValue(room, activated.RoomName);
                    roomType.GetProperty("IsActive")?.SetValue(room, true);
                    roomType.GetProperty("ActivatedAt")?.SetValue(room, activated.OccurredAt);
                    roomType.GetProperty("DeactivatedAt")?.SetValue(room, null);
                    break;
                case ConsultingRoomDeactivated deactivated:
                    roomType.GetProperty("IsActive")?.SetValue(room, false);
                    roomType.GetProperty("DeactivatedAt")?.SetValue(room, deactivated.OccurredAt);
                    roomType.GetProperty("CurrentPatientId")?.SetValue(room, null);
                    roomType.GetProperty("CurrentConsultantId")?.SetValue(room, null);
                    break;
                case PatientClaimedForAttention claimed:
                    roomType.GetProperty("CurrentPatientId")?.SetValue(room, claimed.PatientId);
                    break;
                case PatientAttentionCompleted:
                case PatientAbsentAtConsultation:
                    roomType.GetProperty("CurrentPatientId")?.SetValue(room, null);
                    roomType.GetProperty("CurrentConsultantId")?.SetValue(room, null);
                    break;
            }
        }

        room.SetPersistedVersion(events.Count);
        room.ClearUnraisedEvents();
        return room;
    }
}
