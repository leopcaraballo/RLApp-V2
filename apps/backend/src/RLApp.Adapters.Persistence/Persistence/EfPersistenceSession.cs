using RLApp.Adapters.Persistence.Data;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Persistence;

public class EfPersistenceSession : IPersistenceSession
{
    private readonly AppDbContext _context;

    public EfPersistenceSession(AppDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public void DiscardChanges() =>
        _context.ChangeTracker.Clear();
}
