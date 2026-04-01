using RLApp.Adapters.Persistence.Persistence;

namespace RLApp.Infrastructure.BackgroundServices;

public sealed class OutboxProcessingSignal : IOutboxProcessingSignal
{
    private readonly SemaphoreSlim _semaphore = new(0);

    public void NotifyNewMessages()
    {
        _semaphore.Release();
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return _semaphore.WaitAsync(timeout, cancellationToken);
    }
}
