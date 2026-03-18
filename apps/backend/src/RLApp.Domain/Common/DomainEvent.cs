namespace RLApp.Domain.Common;

/// <summary>
/// Base class for all domain events.
/// Reference: ADR-003 Event Sourcing and CQRS
/// </summary>
public abstract class DomainEvent
{
    public string EventType { get; protected set; }
    public DateTime OccurredAt { get; protected set; }
    public string CorrelationId { get; protected set; }
    public string AggregateId { get; protected set; }

    protected DomainEvent(string eventType, string aggregateId, string correlationId)
    {
        EventType = eventType;
        AggregateId = aggregateId;
        CorrelationId = correlationId;
        OccurredAt = DateTime.UtcNow;
    }
}
