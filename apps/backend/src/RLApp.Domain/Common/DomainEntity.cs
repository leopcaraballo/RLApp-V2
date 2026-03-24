namespace RLApp.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Reference: ADR-001 Hexagonal Architecture
/// </summary>
public abstract class DomainEntity
{
    public string Id { get; protected set; }
    protected List<DomainEvent> _unraisedEvents = new();

    protected DomainEntity(string id)
    {
        Id = id;
    }

    public IReadOnlyList<DomainEvent> GetUnraisedEvents() => _unraisedEvents.AsReadOnly();

    public void ClearUnraisedEvents() => _unraisedEvents.Clear();

    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _unraisedEvents.Add(domainEvent);
    }
}
