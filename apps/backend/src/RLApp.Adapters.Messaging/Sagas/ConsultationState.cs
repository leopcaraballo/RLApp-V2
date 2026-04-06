using Automatonymous;

namespace RLApp.Adapters.Messaging.Sagas;

/// <summary>
/// State data for the Consultation Saga.
/// </summary>
public class ConsultationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public string? TrajectoryId { get; set; }
    public string LastCorrelationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string QueueId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;

    public DateTime? CalledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    // For Timeout handling
    public Guid? TimeoutTokenId { get; set; }
}
