using MassTransit;
using RLApp.Domain.Events;
using System.Security.Cryptography;
using System.Text;

namespace RLApp.Adapters.Messaging.Sagas;

/// <summary>
/// State machine for coordinating the patient consultation flow.
/// </summary>
public class ConsultationSaga : MassTransitStateMachine<ConsultationState>
{
    // States
    public State WaitingForPatient { get; private set; }
    public State InConsultation { get; private set; }
    public State Expired { get; private set; }

    // Events
    public Event<PatientCalled> PatientCalled { get; private set; }
    public Event<PatientAttentionCompleted> AttentionCompleted { get; private set; }
    public Event<PatientAbsentAtConsultation> PatientAbsent { get; private set; }

    public static Guid BuildSagaCorrelationId(string? trajectoryId, string patientId)
    {
        var key = !string.IsNullOrWhiteSpace(trajectoryId)
            ? $"trajectory:{trajectoryId.Trim()}"
            : $"legacy-patient:{patientId.Trim()}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash[..16]);
    }

    public ConsultationSaga()
    {
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
                .Then(context =>
                {
                    context.Saga.TrajectoryId = context.Message.TrajectoryId ?? string.Empty;
                    context.Saga.LastCorrelationId = context.Message.CorrelationId;
                    context.Saga.PatientId = context.Message.PatientId;
                    context.Saga.QueueId = context.Message.AggregateId;
                    context.Saga.RoomId = context.Message.RoomId;
                    context.Saga.CalledAt = context.Message.OccurredAt;
                    context.Saga.LastUpdatedAt = context.Message.OccurredAt;
                })
                .TransitionTo(WaitingForPatient)
        );

        During(WaitingForPatient,
            When(AttentionCompleted) // Assuming attention started and ended
                .Then(context =>
                {
                    if (!string.IsNullOrWhiteSpace(context.Message.TrajectoryId))
                    {
                        context.Saga.TrajectoryId = context.Message.TrajectoryId;
                    }

                    context.Saga.LastCorrelationId = context.Message.CorrelationId;
                    context.Saga.StartedAt = context.Message.OccurredAt; // Simplified: usually there's an 'AttentionStarted' event
                    context.Saga.LastUpdatedAt = context.Message.OccurredAt;
                })
                .TransitionTo(InConsultation)
                .Finalize(), // Simplified for now

            When(PatientAbsent)
                .Then(context =>
                {
                    if (!string.IsNullOrWhiteSpace(context.Message.TrajectoryId))
                    {
                        context.Saga.TrajectoryId = context.Message.TrajectoryId;
                    }

                    context.Saga.LastCorrelationId = context.Message.CorrelationId;
                    context.Saga.LastUpdatedAt = context.Message.OccurredAt;
                })
                .TransitionTo(Expired)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
