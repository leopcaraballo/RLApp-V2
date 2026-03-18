namespace RLApp.Ports.Outbound;

/// <summary>
/// Port for audit trail persistence.
/// Reference: S-007 Reporting and Audit
/// </summary>
public interface IAuditStore
{
    Task RecordAsync(string actor, string action, string entity, string entityId, object data, string correlationId, bool success, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<IList<object>> GetAuditTrailByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
}
