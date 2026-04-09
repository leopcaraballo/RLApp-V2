namespace RLApp.Domain.Events;

using Common;
using System.Text.Json.Serialization;

/// <summary>
/// EV-008 ConsultingRoomActivated
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class ConsultingRoomActivated : DomainEvent
{
    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [JsonPropertyName("roomName")]
    public string RoomName { get; set; } = string.Empty;

    public ConsultingRoomActivated(string aggregateId, string roomName, string correlationId)
        : base(nameof(ConsultingRoomActivated), aggregateId, correlationId)
    {
        RoomId = aggregateId;
        RoomName = roomName;
    }

    protected ConsultingRoomActivated() { }
}

/// <summary>
/// EV-009 ConsultingRoomDeactivated
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class ConsultingRoomDeactivated : DomainEvent
{
    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    public ConsultingRoomDeactivated(string aggregateId, string correlationId)
        : base(nameof(ConsultingRoomDeactivated), aggregateId, correlationId)
    {
        RoomId = aggregateId;
    }

    protected ConsultingRoomDeactivated() { }
}

/// <summary>
/// EV-010 PatientClaimedForAttention
/// Raised when a patient is claimed for attention.
/// </summary>
public class PatientClaimedForAttention : DomainEvent
{
    public const string ClaimedPhase = "Claimed";
    public const string StartedPhase = "Started";

    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [JsonPropertyName("consultationPhase")]
    public string? ConsultationPhase { get; set; }

    [JsonIgnore]
    public bool RepresentsStartedAttention => !string.Equals(ConsultationPhase, ClaimedPhase, StringComparison.OrdinalIgnoreCase);

    public PatientClaimedForAttention(
        string aggregateId,
        string patientId,
        string roomId,
        string correlationId,
        string? trajectoryId = null,
        string consultationPhase = StartedPhase)
        : base(nameof(PatientClaimedForAttention), aggregateId, correlationId)
    {
        PatientId = patientId;
        RoomId = roomId;
        TrajectoryId = trajectoryId;
        ConsultationPhase = consultationPhase;
    }

    protected PatientClaimedForAttention() { }
}

/// <summary>
/// EV-011 PatientCalled
/// Raised when a patient is called for consultation.
/// </summary>
public class PatientCalled : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    public PatientCalled(string aggregateId, string patientId, string roomId, string correlationId, string? trajectoryId)
        : base(nameof(PatientCalled), aggregateId, correlationId)
    {
        PatientId = patientId;
        RoomId = roomId;
        TrajectoryId = trajectoryId;
    }

    protected PatientCalled() { }
}

/// <summary>
/// EV-012 PatientAttentionCompleted
/// Raised when patient attention is completed.
/// </summary>
public class PatientAttentionCompleted : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    [JsonPropertyName("outcome")]
    public string? Outcome { get; set; }

    public PatientAttentionCompleted(
        string aggregateId,
        string patientId,
        string roomId,
        string? turnId,
        string? outcome,
        string correlationId,
        string? trajectoryId)
        : base(nameof(PatientAttentionCompleted), aggregateId, correlationId)
    {
        PatientId = patientId;
        RoomId = roomId;
        TurnId = turnId;
        Outcome = outcome;
        TrajectoryId = trajectoryId;
    }

    protected PatientAttentionCompleted() { }
}

/// <summary>
/// EV-013 PatientAbsentAtConsultation
/// Raised when a patient is absent during consultation.
/// </summary>
public class PatientAbsentAtConsultation : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    public PatientAbsentAtConsultation(string aggregateId, string patientId, string? turnId, string? reason, string correlationId, string? trajectoryId)
        : base(nameof(PatientAbsentAtConsultation), aggregateId, correlationId)
    {
        PatientId = patientId;
        TurnId = turnId;
        Reason = reason;
        TrajectoryId = trajectoryId;
    }

    protected PatientAbsentAtConsultation() { }
}
