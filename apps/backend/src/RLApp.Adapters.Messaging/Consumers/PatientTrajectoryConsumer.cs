using MassTransit;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;
using RLApp.Domain.Events;

namespace RLApp.Adapters.Messaging.Consumers;

public sealed class PatientTrajectoryConsumer :
    IConsumer<PatientTrajectoryOpened>,
    IConsumer<PatientTrajectoryStageRecorded>,
    IConsumer<PatientTrajectoryCompleted>,
    IConsumer<PatientTrajectoryCancelled>,
    IConsumer<PatientTrajectoryRebuilt>
{
    private readonly IPatientTrajectoryRepository _trajectoryRepository;
    private readonly IProjectionStore _projectionStore;

    public PatientTrajectoryConsumer(
        IPatientTrajectoryRepository trajectoryRepository,
        IProjectionStore projectionStore)
    {
        _trajectoryRepository = trajectoryRepository;
        _projectionStore = projectionStore;
    }

    public Task Consume(ConsumeContext<PatientTrajectoryOpened> context)
        => RefreshAsync(context.Message.AggregateId, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryStageRecorded> context)
        => RefreshAsync(context.Message.AggregateId, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryCompleted> context)
        => RefreshAsync(context.Message.AggregateId, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryCancelled> context)
        => RefreshAsync(context.Message.AggregateId, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryRebuilt> context)
        => RefreshAsync(context.Message.AggregateId, context.CancellationToken);

    private async Task RefreshAsync(string trajectoryId, CancellationToken cancellationToken)
    {
        var trajectory = await _trajectoryRepository.GetByIdAsync(trajectoryId, cancellationToken);

        await _projectionStore.UpsertAsync(trajectoryId, "PatientTrajectory", new PatientTrajectoryProjection
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
        }, cancellationToken);
    }
}
