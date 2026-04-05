namespace RLApp.Ports.Outbound;

using RLApp.Domain.Common;

/// <summary>
/// Port for event store persistence.
/// Reference: ADR-003 Event Sourcing and CQRS
/// </summary>
public interface IEventStore
{
    Task SaveAsync(DomainEvent domainEvent, int? expectedVersion = null, CancellationToken cancellationToken = default);
    Task SaveBatchAsync(IEnumerable<DomainEvent> domainEvents, int? expectedVersion = null, CancellationToken cancellationToken = default);
    Task<IList<DomainEvent>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<IList<DomainEvent>> GetEventsByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IList<DomainEvent>> GetAllAsync(CancellationToken cancellationToken = default);
}
