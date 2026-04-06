using Automatonymous;
using MassTransit;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Messaging.Observability;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using System.Security.Cryptography;
using System.Text;

namespace RLApp.Adapters.Messaging.Sagas;

/// <summary>
/// State machine for coordinating the patient consultation flow.
/// </summary>
public class ConsultationSaga : MassTransitStateMachine<ConsultationState>
{
    private readonly ILogger<ConsultationSaga> _logger;

    // States
    public State WaitingForPatient { get; private set; } = null!;
    public State InConsultation { get; private set; } = null!;
    public State Expired { get; private set; } = null!;

    // Events
    public Event<PatientCalled> PatientCalled { get; private set; } = null!;
    public Event<PatientAttentionCompleted> AttentionCompleted { get; private set; } = null!;
    public Event<PatientAbsentAtConsultation> PatientAbsent { get; private set; } = null!;

    public static Guid BuildSagaCorrelationId(string? trajectoryId, string patientId)
    {
        var key = !string.IsNullOrWhiteSpace(trajectoryId)
            ? $"trajectory:{trajectoryId.Trim()}"
            : $"legacy-patient:{patientId.Trim()}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash[..16]);
    }

    public ConsultationSaga(ILogger<ConsultationSaga> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => PatientCalled, x =>
        {
            x.CorrelateById(context => BuildSagaCorrelationId(context.Message.TrajectoryId, context.Message.PatientId));
            x.SelectId(context => BuildSagaCorrelationId(context.Message.TrajectoryId, context.Message.PatientId));
        });

        Event(() => AttentionCompleted, x =>
        {
            x.CorrelateById(context => BuildSagaCorrelationId(context.Message.TrajectoryId, context.Message.PatientId));
            x.OnMissingInstance(m => m.Discard());
        });

        Event(() => PatientAbsent, x =>
        {
            x.CorrelateById(context => BuildSagaCorrelationId(context.Message.TrajectoryId, context.Message.PatientId));
            x.OnMissingInstance(m => m.Discard());
        });

        Initially(
            When(PatientCalled)
                .Then(context => ApplyTransition(context, nameof(WaitingForPatient), () =>
                {
                    context.Instance.TrajectoryId = context.Data.TrajectoryId;
                    context.Instance.LastCorrelationId = context.Data.CorrelationId;
                    context.Instance.PatientId = context.Data.PatientId;
                    context.Instance.QueueId = context.Data.AggregateId;
                    context.Instance.RoomId = context.Data.RoomId;
                    context.Instance.CalledAt = context.Data.OccurredAt;
                    context.Instance.LastUpdatedAt = context.Data.OccurredAt;
                }))
                .TransitionTo(WaitingForPatient)
        );

        During(WaitingForPatient,
            When(AttentionCompleted) // Assuming attention started and ended
                .Then(context => ApplyTransition(context, nameof(InConsultation), () =>
                {
                    if (!string.IsNullOrWhiteSpace(context.Data.TrajectoryId))
                    {
                        context.Instance.TrajectoryId = context.Data.TrajectoryId;
                    }

                    context.Instance.LastCorrelationId = context.Data.CorrelationId;
                    context.Instance.StartedAt = context.Data.OccurredAt; // Simplified: usually there's an 'AttentionStarted' event
                    context.Instance.LastUpdatedAt = context.Data.OccurredAt;
                }))
                .TransitionTo(InConsultation)
                .Finalize(), // Simplified for now

            When(PatientAbsent)
                .Then(context => ApplyTransition(context, nameof(Expired), () =>
                {
                    if (!string.IsNullOrWhiteSpace(context.Data.TrajectoryId))
                    {
                        context.Instance.TrajectoryId = context.Data.TrajectoryId;
                    }

                    context.Instance.LastCorrelationId = context.Data.CorrelationId;
                    context.Instance.LastUpdatedAt = context.Data.OccurredAt;
                }))
                .TransitionTo(Expired)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }

    private void ApplyTransition<TMessage>(BehaviorContext<ConsultationState, TMessage> context, string nextState, Action updateSagaState)
        where TMessage : DomainEvent
    {
        var currentState = MessageFlowTelemetry.NormalizeState(context.Instance.CurrentState);

        using var activity = MessageFlowTelemetry.StartSagaActivity(
            context.Data,
            nameof(ConsultationSaga),
            currentState,
            nextState);
        using var scope = MessageFlowTelemetry.BeginScope(
            _logger,
            context.Data,
            "transition-pending",
            sagaName: nameof(ConsultationSaga),
            currentState: currentState,
            nextState: nextState);

        try
        {
            updateSagaState();
            MessageFlowTelemetry.SetResult(activity, "transition-applied");
            _logger.LogInformation("Consultation saga transition applied.");
        }
        catch (Exception ex)
        {
            MessageFlowTelemetry.RecordFailure(activity, ex, "transition-failed");
            _logger.LogError(ex, "Consultation saga transition failed.");
            throw;
        }
    }
}
