namespace RLApp.Adapters.Persistence.Persistence;

public interface IOutboxProcessingSignal
{
    void NotifyNewMessages();

    Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
