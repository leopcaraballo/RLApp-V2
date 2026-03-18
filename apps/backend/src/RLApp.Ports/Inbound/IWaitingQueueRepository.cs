namespace RLApp.Ports.Inbound;

using RLApp.Domain.Aggregates;

/// <summary>
/// Port for waiting queue repository.
/// Reference: S-002 Consulting Room Lifecycle, S-003 Queue Open and Check-in
/// </summary>
public interface IWaitingQueueRepository
{
    Task<WaitingQueue> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(WaitingQueue waitingQueue, CancellationToken cancellationToken = default);
    Task UpdateAsync(WaitingQueue waitingQueue, CancellationToken cancellationToken = default);
    Task<IList<WaitingQueue>> GetAllAsync(CancellationToken cancellationToken = default);
}
