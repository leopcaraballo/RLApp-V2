using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Persistence;

public class EfPersistenceSession : IPersistenceSession
{
    private readonly AppDbContext _context;
    private readonly IOutboxProcessingSignal _outboxProcessingSignal;

    public EfPersistenceSession(AppDbContext context, IOutboxProcessingSignal outboxProcessingSignal)
    {
        _context = context;
        _outboxProcessingSignal = outboxProcessingSignal;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var hasPendingOutboxMessages = _context.ChangeTracker
            .Entries<OutboxMessage>()
            .Any(entry => entry.State == EntityState.Added);

        await _context.SaveChangesAsync(cancellationToken);

        if (hasPendingOutboxMessages)
        {
            _outboxProcessingSignal.NotifyNewMessages();
        }
    }

    public void DiscardChanges() =>
        _context.ChangeTracker.Clear();
}
