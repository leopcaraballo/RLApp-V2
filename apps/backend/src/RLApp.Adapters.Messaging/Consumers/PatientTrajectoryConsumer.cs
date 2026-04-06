using MassTransit;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Messaging.Observability;
using RLApp.Domain.Common;
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
    private readonly ILogger<PatientTrajectoryConsumer> _logger;

    public PatientTrajectoryConsumer(
        IPatientTrajectoryRepository trajectoryRepository,
        IProjectionStore projectionStore,
        ILogger<PatientTrajectoryConsumer> logger)
    {
        _trajectoryRepository = trajectoryRepository;
        _projectionStore = projectionStore;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<PatientTrajectoryOpened> context)
        => RefreshAsync(context.Message.AggregateId, context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryStageRecorded> context)
        => RefreshAsync(context.Message.AggregateId, context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryCompleted> context)
        => RefreshAsync(context.Message.AggregateId, context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryCancelled> context)
        => RefreshAsync(context.Message.AggregateId, context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<PatientTrajectoryRebuilt> context)
        => RefreshAsync(context.Message.AggregateId, context.Message, context.CancellationToken);

    private async Task RefreshAsync(string trajectoryId, DomainEvent message, CancellationToken cancellationToken)
    {
        using var activity = MessageFlowTelemetry.StartConsumerActivity(message, nameof(PatientTrajectoryConsumer));
        using var scope = MessageFlowTelemetry.BeginScope(
            _logger,
            message,
            "projection-pending",
            consumerName: nameof(PatientTrajectoryConsumer));

        try
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

            MessageFlowTelemetry.SetResult(activity, "projection-upserted");
            _logger.LogInformation("Patient trajectory projection refreshed.");
        }
        catch (Exception ex)
        {
            MessageFlowTelemetry.RecordFailure(activity, ex, "projection-failed");
            _logger.LogError(ex, "Patient trajectory projection refresh failed.");
            throw;
        }
    }
}
