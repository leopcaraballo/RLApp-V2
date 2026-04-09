namespace RLApp.Domain.Common;

using System.Text.Json.Serialization;

/// <summary>
/// Base class for all domain events.
/// Reference: ADR-003 Event Sourcing and CQRS
/// </summary>
public abstract class DomainEvent
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; }

    [JsonPropertyName("aggregateId")]
    public string AggregateId { get; set; }

    [JsonPropertyName("trajectoryId")]
    public string? TrajectoryId { get; set; }

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    protected DomainEvent(string eventType, string aggregateId, string correlationId)
    {
        EventType = eventType;
        AggregateId = aggregateId;
        CorrelationId = correlationId;
        OccurredAt = DateTime.UtcNow;
    }

    protected DomainEvent() { }
}
