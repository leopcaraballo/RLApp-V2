namespace RLApp.Domain.Events;

using Common;
using System.Text.Json.Serialization;

public sealed class PatientTrajectoryOpened : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    public PatientTrajectoryOpened(string aggregateId, string patientId, string queueId, DateTime occurredAt, string correlationId)
        : base(nameof(PatientTrajectoryOpened), aggregateId, correlationId)
    {
        PatientId = patientId;
        QueueId = queueId;
        OccurredAt = occurredAt;
    }

    private PatientTrajectoryOpened() { }
}

public sealed class PatientTrajectoryStageRecorded : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [JsonPropertyName("stage")]
    public string Stage { get; set; } = string.Empty;

    [JsonPropertyName("sourceEvent")]
    public string SourceEvent { get; set; } = string.Empty;

    [JsonPropertyName("sourceState")]
    public string? SourceState { get; set; }

    public PatientTrajectoryStageRecorded(
        string aggregateId,
        string patientId,
        string queueId,
        string stage,
        string sourceEvent,
        string? sourceState,
        DateTime occurredAt,
        string correlationId)
        : base(nameof(PatientTrajectoryStageRecorded), aggregateId, correlationId)
    {
        PatientId = patientId;
        QueueId = queueId;
        Stage = stage;
        SourceEvent = sourceEvent;
        SourceState = sourceState;
        OccurredAt = occurredAt;
    }

    private PatientTrajectoryStageRecorded() { }
}

public sealed class PatientTrajectoryCompleted : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [JsonPropertyName("stage")]
    public string Stage { get; set; } = string.Empty;

    [JsonPropertyName("sourceEvent")]
    public string SourceEvent { get; set; } = string.Empty;

    [JsonPropertyName("sourceState")]
    public string? SourceState { get; set; }

    public PatientTrajectoryCompleted(
        string aggregateId,
        string patientId,
        string queueId,
        string stage,
        string sourceEvent,
        string? sourceState,
        DateTime occurredAt,
        string correlationId)
        : base(nameof(PatientTrajectoryCompleted), aggregateId, correlationId)
    {
        PatientId = patientId;
        QueueId = queueId;
        Stage = stage;
        SourceEvent = sourceEvent;
        SourceState = sourceState;
        OccurredAt = occurredAt;
    }

    private PatientTrajectoryCompleted() { }
}

public sealed class PatientTrajectoryCancelled : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [JsonPropertyName("sourceEvent")]
    public string SourceEvent { get; set; } = string.Empty;

    [JsonPropertyName("sourceState")]
    public string? SourceState { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    public PatientTrajectoryCancelled(
        string aggregateId,
        string patientId,
        string queueId,
        string sourceEvent,
        string? sourceState,
        string? reason,
        DateTime occurredAt,
        string correlationId)
        : base(nameof(PatientTrajectoryCancelled), aggregateId, correlationId)
    {
        PatientId = patientId;
        QueueId = queueId;
        SourceEvent = sourceEvent;
        SourceState = sourceState;
        Reason = reason;
        OccurredAt = occurredAt;
    }

    private PatientTrajectoryCancelled() { }
}

public sealed class PatientTrajectoryRebuilt : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    public PatientTrajectoryRebuilt(
        string aggregateId,
        string patientId,
        string queueId,
        string scope,
        DateTime occurredAt,
        string correlationId)
        : base(nameof(PatientTrajectoryRebuilt), aggregateId, correlationId)
    {
        PatientId = patientId;
        QueueId = queueId;
        Scope = scope;
        OccurredAt = occurredAt;
    }

    private PatientTrajectoryRebuilt() { }
}
