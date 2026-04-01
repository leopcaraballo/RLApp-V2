namespace RLApp.Infrastructure.BackgroundServices;

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(object eventPayload, Type eventType, CancellationToken cancellationToken);
}
