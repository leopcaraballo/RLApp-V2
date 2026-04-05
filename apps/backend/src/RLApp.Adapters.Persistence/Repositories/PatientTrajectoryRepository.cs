using System.Reflection;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

public sealed class PatientTrajectoryRepository : IPatientTrajectoryRepository
{
    private static readonly PropertyInfo CurrentStateProperty = typeof(PatientTrajectory)
        .GetProperty(nameof(PatientTrajectory.CurrentState), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    private static readonly PropertyInfo OpenedAtProperty = typeof(PatientTrajectory)
        .GetProperty(nameof(PatientTrajectory.OpenedAt), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    private static readonly PropertyInfo ClosedAtProperty = typeof(PatientTrajectory)
        .GetProperty(nameof(PatientTrajectory.ClosedAt), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    private readonly IEventStore _eventStore;

    public PatientTrajectoryRepository(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<PatientTrajectory> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var events = (await _eventStore.GetEventsByAggregateIdAsync(id, cancellationToken))
            .Where(IsTrajectoryEvent)
            .OrderBy(@event => @event.OccurredAt)
            .ToList();

        if (events.Count == 0)
        {
            throw new KeyNotFoundException($"Patient trajectory {id} not found");
        }

        return Replay(events);
    }

    public async Task<PatientTrajectory?> FindActiveAsync(string patientId, string queueId, CancellationToken cancellationToken = default)
    {
        var candidateIds = (await _eventStore.GetAllAsync(cancellationToken))
            .OfType<PatientTrajectoryOpened>()
            .Where(@event => string.Equals(@event.PatientId, patientId, StringComparison.Ordinal)
                && string.Equals(@event.QueueId, queueId, StringComparison.Ordinal))
            .OrderByDescending(@event => @event.OccurredAt)
            .Select(@event => @event.AggregateId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var candidateId in candidateIds)
        {
            var trajectory = await GetByIdAsync(candidateId, cancellationToken);
            if (string.Equals(trajectory.CurrentState, PatientTrajectory.ActiveState, StringComparison.Ordinal))
            {
                return trajectory;
            }
        }

        return null;
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

        await _eventStore.SaveBatchAsync(events, cancellationToken);
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

    private static PatientTrajectory Replay(IEnumerable<DomainEvent> events)
    {
        var orderedEvents = events.OrderBy(@event => @event.OccurredAt).ToList();
        var openedEvent = orderedEvents.OfType<PatientTrajectoryOpened>().FirstOrDefault()
            ?? throw new DomainException("Trajectory stream is missing the opening event");

        var trajectory = (PatientTrajectory)Activator.CreateInstance(
            typeof(PatientTrajectory),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new object[] { openedEvent.AggregateId, openedEvent.PatientId, openedEvent.QueueId },
            null)!;

        var stages = trajectory.Stages;
        var correlationIds = trajectory.CorrelationIds;

        foreach (var @event in orderedEvents)
        {
            switch (@event)
            {
                case PatientTrajectoryOpened opened:
                    OpenedAtProperty.SetValue(trajectory, opened.OccurredAt);
                    CurrentStateProperty.SetValue(trajectory, PatientTrajectory.ActiveState);
                    AddCorrelationId(correlationIds, opened.CorrelationId);
                    break;

                case PatientTrajectoryStageRecorded stageRecorded:
                    AddStage(stages, stageRecorded.Stage, stageRecorded.SourceEvent, stageRecorded.SourceState, stageRecorded.OccurredAt, stageRecorded.CorrelationId);
                    AddCorrelationId(correlationIds, stageRecorded.CorrelationId);
                    break;

                case PatientTrajectoryCompleted completed:
                    AddStage(stages, completed.Stage, completed.SourceEvent, completed.SourceState, completed.OccurredAt, completed.CorrelationId);
                    AddCorrelationId(correlationIds, completed.CorrelationId);
                    CurrentStateProperty.SetValue(trajectory, PatientTrajectory.CompletedState);
                    ClosedAtProperty.SetValue(trajectory, completed.OccurredAt);
                    break;

                case PatientTrajectoryCancelled cancelled:
                    AddCorrelationId(correlationIds, cancelled.CorrelationId);
                    CurrentStateProperty.SetValue(trajectory, PatientTrajectory.CancelledState);
                    ClosedAtProperty.SetValue(trajectory, cancelled.OccurredAt);
                    break;

                case PatientTrajectoryRebuilt rebuilt:
                    AddCorrelationId(correlationIds, rebuilt.CorrelationId);
                    break;
            }
        }

        trajectory.ClearUnraisedEvents();
        return trajectory;
    }

    private static void AddCorrelationId(ICollection<string> correlationIds, string correlationId)
    {
        if (!correlationIds.Contains(correlationId, StringComparer.Ordinal))
        {
            correlationIds.Add(correlationId);
        }
    }

    private static void AddStage(
        ICollection<TrajectoryStage> stages,
        string stage,
        string sourceEvent,
        string? sourceState,
        DateTime occurredAt,
        string correlationId)
    {
        var duplicate = stages.Any(existing =>
            string.Equals(existing.Stage, stage, StringComparison.Ordinal)
            && string.Equals(existing.SourceEvent, sourceEvent, StringComparison.Ordinal)
            && string.Equals(existing.SourceState, sourceState, StringComparison.Ordinal)
            && existing.OccurredAt == occurredAt
            && string.Equals(existing.CorrelationId, correlationId, StringComparison.Ordinal));

        if (duplicate)
        {
            return;
        }

        stages.Add(new TrajectoryStage
        {
            Stage = stage,
            SourceEvent = sourceEvent,
            SourceState = sourceState,
            OccurredAt = occurredAt,
            CorrelationId = correlationId
        });
    }
}
