using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

public sealed class PatientTrajectoryRepository : IPatientTrajectoryRepository
{
    private readonly IEventStore _eventStore;
    private readonly IProjectionStore _projectionStore;

    public PatientTrajectoryRepository(IEventStore eventStore, IProjectionStore projectionStore)
    {
        _eventStore = eventStore;
        _projectionStore = projectionStore;
    }

    public async Task<PatientTrajectory> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var events = (await _eventStore.GetEventsByAggregateIdAsync(id, cancellationToken))
            .Where(IsTrajectoryEvent)
            .ToList();

        if (events.Count == 0)
        {
            throw new KeyNotFoundException($"Patient trajectory {id} not found");
        }

        return PatientTrajectory.Replay(events);
    }

    public async Task<PatientTrajectory?> FindActiveAsync(string patientId, string queueId, CancellationToken cancellationToken = default)
    {
        var projections = await _projectionStore.FindPatientTrajectoriesAsync(patientId, queueId, cancellationToken);

        var activeProjection = projections
            .FirstOrDefault(p => string.Equals(p.CurrentState, PatientTrajectory.ActiveState, StringComparison.Ordinal));

        if (activeProjection is null)
        {
            return null;
        }

        return await GetByIdAsync(activeProjection.TrajectoryId, cancellationToken);
    }

    public Task AddAsync(PatientTrajectory trajectory, CancellationToken cancellationToken = default)
        => SavePendingEventsAsync(trajectory, cancellationToken);

    public Task UpdateAsync(PatientTrajectory trajectory, CancellationToken cancellationToken = default)
        => SavePendingEventsAsync(trajectory, cancellationToken);

    private async Task SavePendingEventsAsync(PatientTrajectory trajectory, CancellationToken cancellationToken)
    {
        var events = trajectory.GetUnraisedEvents();
        if (events.Count == 0)
        {
            return;
        }

        await _eventStore.SaveBatchAsync(events, trajectory.Version, cancellationToken);
        trajectory.SetPersistedVersion(trajectory.Version + events.Count);
    }

    private static bool IsTrajectoryEvent(DomainEvent @event) => @event switch
    {
        PatientTrajectoryOpened => true,
        PatientTrajectoryStageRecorded => true,
        PatientTrajectoryCompleted => true,
        PatientTrajectoryCancelled => true,
        PatientTrajectoryRebuilt => true,
        _ => false
    };
}
