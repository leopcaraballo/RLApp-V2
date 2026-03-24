namespace RLApp.Ports.Inbound;

using RLApp.Domain.Common;

/// <summary>
/// Port for event publishing.
/// Used by application services to publish domain events.
/// Reference: ADR-005 RabbitMQ and Outbox
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
