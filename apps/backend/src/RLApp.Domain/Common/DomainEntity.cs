namespace RLApp.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Reference: ADR-001 Hexagonal Architecture
/// </summary>
public abstract class DomainEntity
{
    public string Id { get; protected set; }
    public int Version { get; private set; }
    protected List<DomainEvent> _unraisedEvents = new();

    protected DomainEntity(string id)
    {
        Id = id;
    }

    public IReadOnlyList<DomainEvent> GetUnraisedEvents() => _unraisedEvents.AsReadOnly();

    public void ClearUnraisedEvents() => _unraisedEvents.Clear();

    public void SetPersistedVersion(int version)
    {
        if (version < 0)
            throw new ArgumentOutOfRangeException(nameof(version), "Persisted version cannot be negative");

        Version = version;
    }

    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _unraisedEvents.Add(domainEvent);
    }
}
