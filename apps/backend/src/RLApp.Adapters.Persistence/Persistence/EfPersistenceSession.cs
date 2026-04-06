using Microsoft.EntityFrameworkCore;
using Npgsql;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Domain.Common;
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

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsAggregateSequenceConflict(ex, out var aggregateId, out var expectedVersion))
        {
            throw DomainException.ConcurrencyConflict(aggregateId, expectedVersion, null);
        }

        if (hasPendingOutboxMessages)
        {
            _outboxProcessingSignal.NotifyNewMessages();
        }
    }

    public void DiscardChanges() =>
        _context.ChangeTracker.Clear();

    private bool IsAggregateSequenceConflict(DbUpdateException exception, out string aggregateId, out int expectedVersion)
    {
        aggregateId = string.Empty;
        expectedVersion = 0;

        if (exception.InnerException is not PostgresException postgresException
            || postgresException.SqlState != PostgresErrorCodes.UniqueViolation
            || !string.Equals(postgresException.ConstraintName, EventRecord.AggregateSequenceIndexName, StringComparison.Ordinal))
        {
            return false;
        }

        var conflictingRecord = _context.ChangeTracker
            .Entries<EventRecord>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .OrderBy(entity => entity.OccurredAt)
            .FirstOrDefault();

        if (conflictingRecord is null)
        {
            aggregateId = "UNKNOWN";
            return true;
        }

        aggregateId = conflictingRecord.AggregateId;
        expectedVersion = Math.Max(conflictingRecord.SequenceNumber - 1, 0);
        return true;
    }
}
