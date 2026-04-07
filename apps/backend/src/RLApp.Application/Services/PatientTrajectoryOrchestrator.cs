namespace RLApp.Application.Services;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;

public sealed class PatientTrajectoryOrchestrator
{
    private readonly IPatientTrajectoryRepository _trajectoryRepository;
    private readonly IEventPublisher _eventPublisher;

    public PatientTrajectoryOrchestrator(
        IPatientTrajectoryRepository trajectoryRepository,
        IEventPublisher eventPublisher)
    {
        _trajectoryRepository = trajectoryRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task TrackCheckInAsync(string queueId, PatientCheckedIn @event, CancellationToken cancellationToken)
    {
        var activeTrajectory = await _trajectoryRepository.FindActiveAsync(@event.PatientId, queueId, cancellationToken);
        if (activeTrajectory is not null)
            throw new DomainException("Patient already has an active trajectory in this queue");

        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(queueId, @event.PatientId, @event.OccurredAt),
            @event.PatientId,
            queueId,
            PatientTrajectory.ReceptionStage,
            @event.EventType,
            "EnEsperaTaquilla",
            @event.OccurredAt,
            @event.CorrelationId);

        await PersistAndPublishAsync(trajectory, isNew: true, cancellationToken);
    }

    public async Task TrackPaymentValidatedAsync(string queueId, PatientPaymentValidated @event, CancellationToken cancellationToken)
    {
        var (trajectory, isNew) = await GetOrCreateActiveTrajectoryAsync(
            queueId,
            @event.PatientId,
            PatientTrajectory.CashierStage,
            @event.EventType,
            "EnEsperaConsulta",
            @event.OccurredAt,
            @event.CorrelationId,
            cancellationToken);

        if (!isNew && !trajectory.RecordStage(
                PatientTrajectory.CashierStage,
                @event.EventType,
                "EnEsperaConsulta",
                @event.OccurredAt,
                @event.CorrelationId))
        {
            return;
        }

        await PersistAndPublishAsync(trajectory, isNew, cancellationToken);
    }

    public async Task TrackConsultationStartedAsync(string queueId, PatientClaimedForAttention @event, CancellationToken cancellationToken)
    {
        if (!@event.RepresentsStartedAttention)
        {
            return;
        }

        var (trajectory, isNew) = await GetOrCreateActiveTrajectoryAsync(
            queueId,
            @event.PatientId,
            PatientTrajectory.ConsultationStage,
            @event.EventType,
            "EnConsulta",
            @event.OccurredAt,
            @event.CorrelationId,
            cancellationToken);

        if (!isNew && !trajectory.RecordStage(
                PatientTrajectory.ConsultationStage,
                @event.EventType,
                "EnConsulta",
                @event.OccurredAt,
                @event.CorrelationId))
        {
            return;
        }

        await PersistAndPublishAsync(trajectory, isNew, cancellationToken);
    }

    public async Task TrackCompletionAsync(string queueId, PatientAttentionCompleted @event, CancellationToken cancellationToken)
    {
        var (trajectory, isNew) = await GetOrCreateActiveTrajectoryAsync(
            queueId,
            @event.PatientId,
            PatientTrajectory.ConsultationStage,
            @event.EventType,
            "Finalizado",
            @event.OccurredAt,
            @event.CorrelationId,
            cancellationToken);

        if (!trajectory.Complete(
                PatientTrajectory.ConsultationStage,
                @event.EventType,
                "Finalizado",
                @event.OccurredAt,
                @event.CorrelationId))
        {
            return;
        }

        await PersistAndPublishAsync(trajectory, isNew, cancellationToken);
    }

    public async Task TrackCashierAbsenceAsync(string queueId, PatientAbsentAtCashier @event, CancellationToken cancellationToken)
    {
        var (trajectory, isNew) = await GetOrCreateActiveTrajectoryAsync(
            queueId,
            @event.PatientId,
            PatientTrajectory.CashierStage,
            @event.EventType,
            "CanceladoPorAusencia",
            @event.OccurredAt,
            @event.CorrelationId,
            cancellationToken);

        if (!trajectory.Cancel(@event.EventType, "CanceladoPorAusencia", @event.Reason, @event.OccurredAt, @event.CorrelationId))
        {
            return;
        }

        await PersistAndPublishAsync(trajectory, isNew, cancellationToken);
    }

    public async Task TrackConsultationAbsenceAsync(string queueId, PatientAbsentAtConsultation @event, CancellationToken cancellationToken)
    {
        var (trajectory, isNew) = await GetOrCreateActiveTrajectoryAsync(
            queueId,
            @event.PatientId,
            PatientTrajectory.ConsultationStage,
            @event.EventType,
            "CanceladoPorAusencia",
            @event.OccurredAt,
            @event.CorrelationId,
            cancellationToken);

        if (!trajectory.Cancel(@event.EventType, "CanceladoPorAusencia", @event.Reason, @event.OccurredAt, @event.CorrelationId))
        {
            return;
        }

        await PersistAndPublishAsync(trajectory, isNew, cancellationToken);
    }

    private async Task<(PatientTrajectory Trajectory, bool IsNew)> GetOrCreateActiveTrajectoryAsync(
        string queueId,
        string patientId,
        string initialStage,
        string sourceEvent,
        string sourceState,
        DateTime occurredAt,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var trajectory = await _trajectoryRepository.FindActiveAsync(patientId, queueId, cancellationToken);
        if (trajectory is not null)
        {
            return (trajectory, false);
        }

        trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(queueId, patientId, occurredAt),
            patientId,
            queueId,
            initialStage,
            sourceEvent,
            sourceState,
            occurredAt,
            correlationId);

        return (trajectory, true);
    }

    private async Task PersistAndPublishAsync(PatientTrajectory trajectory, bool isNew, CancellationToken cancellationToken)
    {
        var pendingEvents = trajectory.GetUnraisedEvents();
        if (pendingEvents.Count == 0)
        {
            return;
        }

        if (isNew)
        {
            await _trajectoryRepository.AddAsync(trajectory, cancellationToken);
        }
        else
        {
            await _trajectoryRepository.UpdateAsync(trajectory, cancellationToken);
        }

        await _eventPublisher.PublishBatchAsync(pendingEvents, cancellationToken);
        trajectory.ClearUnraisedEvents();
    }
}
