using MassTransit;

namespace RLApp.Infrastructure.BackgroundServices;

public sealed class MassTransitOutboxMessageDispatcher : IOutboxMessageDispatcher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitOutboxMessageDispatcher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task DispatchAsync(object eventPayload, Type eventType, CancellationToken cancellationToken)
    {
        return _publishEndpoint.Publish(eventPayload, eventType, cancellationToken);
    }
}
