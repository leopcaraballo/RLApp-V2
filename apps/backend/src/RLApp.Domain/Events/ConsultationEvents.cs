namespace RLApp.Domain.Events;

using Common;

/// <summary>
/// EV-008 ConsultingRoomActivated
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class ConsultingRoomActivated : DomainEvent
{
    public string RoomId { get; }
    public string RoomName { get; }

    public ConsultingRoomActivated(string roomId, string roomName, string correlationId)
        : base(nameof(ConsultingRoomActivated), roomId, correlationId)
    {
        RoomId = roomId;
        RoomName = roomName;
    }
}

/// <summary>
/// EV-009 ConsultingRoomDeactivated
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class ConsultingRoomDeactivated : DomainEvent
{
    public string RoomId { get; }

    public ConsultingRoomDeactivated(string roomId, string correlationId)
        : base(nameof(ConsultingRoomDeactivated), roomId, correlationId)
    {
        RoomId = roomId;
    }
}

/// <summary>
/// EV-010 PatientClaimedForAttention
/// Raised when a patient is claimed for attention.
/// </summary>
public class PatientClaimedForAttention : DomainEvent
{
    public string PatientId { get; }
    public string RoomId { get; }

    public PatientClaimedForAttention(string queueId, string patientId, string roomId, string correlationId)
        : base(nameof(PatientClaimedForAttention), queueId, correlationId)
    {
        PatientId = patientId;
        RoomId = roomId;
    }
}

/// <summary>
/// EV-011 PatientCalled
/// Raised when a patient is called for consultation.
/// </summary>
public class PatientCalled : DomainEvent
{
    public string PatientId { get; }
    public string RoomId { get; }

    public PatientCalled(string queueId, string patientId, string roomId, string correlationId)
        : base(nameof(PatientCalled), queueId, correlationId)
    {
        PatientId = patientId;
        RoomId = roomId;
    }
}

/// <summary>
/// EV-012 PatientAttentionCompleted
/// Raised when patient attention is completed.
/// </summary>
public class PatientAttentionCompleted : DomainEvent
{
    public string PatientId { get; }
    public string RoomId { get; }

    public PatientAttentionCompleted(string queueId, string patientId, string roomId, string correlationId)
        : base(nameof(PatientAttentionCompleted), queueId, correlationId)
    {
        PatientId = patientId;
        RoomId = roomId;
    }
}

/// <summary>
/// EV-013 PatientAbsentAtConsultation
/// Raised when a patient is absent during consultation.
/// </summary>
public class PatientAbsentAtConsultation : DomainEvent
{
    public string PatientId { get; }

    public PatientAbsentAtConsultation(string queueId, string patientId, string correlationId)
        : base(nameof(PatientAbsentAtConsultation), queueId, correlationId)
    {
        PatientId = patientId;
    }
}

/// <summary>
/// EV-014 PatientCancelledByAbsence
/// Raised when a patient is cancelled due to absence.
/// </summary>
public class PatientCancelledByAbsence : DomainEvent
{
    public string PatientId { get; }

    public PatientCancelledByAbsence(string queueId, string patientId, string correlationId)
        : base(nameof(PatientCancelledByAbsence), queueId, correlationId)
    {
        PatientId = patientId;
    }
}
