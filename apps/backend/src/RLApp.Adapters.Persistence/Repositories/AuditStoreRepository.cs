using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

/// <summary>
/// Implementation of IAuditStore using Entity Framework Core.
/// </summary>
public class AuditStoreRepository : IAuditStore
{
    private readonly AppDbContext _context;

    public AuditStoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(string actor, string action, string entity, string entityId, object data, string correlationId, bool success, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var record = new AuditLogRecord
        {
            Actor = actor,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Payload = JsonSerializer.Serialize(data),
            CorrelationId = correlationId,
            Success = success,
            ErrorMessage = errorMessage,
            OccurredAt = DateTime.UtcNow
        };

        await _context.Set<AuditLogRecord>().AddAsync(record, cancellationToken);
    }

    public async Task<IList<object>> GetAuditTrailByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var logs = await _context.Set<AuditLogRecord>()
            .AsNoTracking()
            .Where(l => l.CorrelationId == correlationId)
            .OrderBy(l => l.OccurredAt)
            .ToListAsync(cancellationToken);

        return logs.Cast<object>().ToList();
    }
}
