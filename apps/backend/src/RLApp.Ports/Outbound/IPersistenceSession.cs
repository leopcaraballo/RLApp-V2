namespace RLApp.Ports.Outbound;

/// <summary>
/// Port for coordinating atomic persistence across repositories, outbox and audit trail.
/// </summary>
public interface IPersistenceSession
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    void DiscardChanges();
}
