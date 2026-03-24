using MassTransit;
using RLApp.Domain.Events;

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

    public ConsultationSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => PatientCalled, x => x.CorrelateBy((saga, context) => saga.PatientId == context.Message.PatientId)
            .SelectId(context => Guid.NewGuid())); // New saga for each call

        Event(() => AttentionCompleted, x => x.CorrelateBy((saga, context) => saga.PatientId == context.Message.PatientId));
        Event(() => PatientAbsent, x => x.CorrelateBy((saga, context) => saga.PatientId == context.Message.PatientId));

        Initially(
            When(PatientCalled)
                .Then(context =>
                {
                    context.Saga.PatientId = context.Message.PatientId;
                    context.Saga.QueueId = context.Message.AggregateId;
                    context.Saga.RoomId = context.Message.RoomId;
                    context.Saga.CalledAt = DateTime.UtcNow;
                    context.Saga.LastUpdatedAt = DateTime.UtcNow;
                })
                .TransitionTo(WaitingForPatient)
        );

        During(WaitingForPatient,
            When(AttentionCompleted) // Assuming attention started and ended
                .Then(context =>
                {
                    context.Saga.StartedAt = DateTime.UtcNow; // Simplified: usually there's an 'AttentionStarted' event
                    context.Saga.LastUpdatedAt = DateTime.UtcNow;
                })
                .TransitionTo(InConsultation)
                .Finalize(), // Simplified for now

            When(PatientAbsent)
                .Then(context =>
                {
                    context.Saga.LastUpdatedAt = DateTime.UtcNow;
                })
                .TransitionTo(Expired)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
