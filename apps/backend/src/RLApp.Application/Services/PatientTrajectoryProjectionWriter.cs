namespace RLApp.Application.Services;

using RLApp.Domain.Aggregates;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

public sealed class PatientTrajectoryProjectionWriter
{
    private readonly IPatientTrajectoryRepository _trajectoryRepository;
    private readonly IProjectionStore _projectionStore;

    public PatientTrajectoryProjectionWriter(
        IPatientTrajectoryRepository trajectoryRepository,
        IProjectionStore projectionStore)
    {
        _trajectoryRepository = trajectoryRepository;
        _projectionStore = projectionStore;
    }

    public async Task RefreshAsync(string trajectoryId, CancellationToken cancellationToken)
    {
        var trajectory = await _trajectoryRepository.GetByIdAsync(trajectoryId, cancellationToken);
        await UpsertAsync(trajectory, cancellationToken);
    }

    public Task UpsertAsync(PatientTrajectory trajectory, CancellationToken cancellationToken)
    {
        return _projectionStore.UpsertAsync(
            trajectory.Id,
            "PatientTrajectory",
            Map(trajectory),
            cancellationToken);
    }

    public static PatientTrajectoryProjection Map(PatientTrajectory trajectory)
    {
        return new PatientTrajectoryProjection
        {
            TrajectoryId = trajectory.Id,
            PatientId = trajectory.PatientId,
            QueueId = trajectory.QueueId,
            CurrentState = trajectory.CurrentState,
            OpenedAt = trajectory.OpenedAt,
            ClosedAt = trajectory.ClosedAt,
            CorrelationIds = trajectory.CorrelationIds.ToArray(),
            Stages = trajectory.Stages
                .OrderBy(stage => stage.OccurredAt)
                .Select(stage => new PatientTrajectoryStageProjection
                {
                    OccurredAt = stage.OccurredAt,
                    Stage = stage.Stage,
                    SourceEvent = stage.SourceEvent,
                    SourceState = stage.SourceState,
                    CorrelationId = stage.CorrelationId
                })
                .ToArray()
        };
    }
}
