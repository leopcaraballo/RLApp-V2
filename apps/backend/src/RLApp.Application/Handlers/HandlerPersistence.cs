namespace RLApp.Application.Handlers;

using RLApp.Ports.Outbound;

internal static class HandlerPersistence
{
    public static async Task CommitSuccessAsync(
        IPersistenceSession persistenceSession,
        IAuditStore auditStore,
        string actor,
        string action,
        string entity,
        string entityId,
        object data,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await auditStore.RecordAsync(actor, action, entity, entityId, data, correlationId, true, cancellationToken: cancellationToken);
        await persistenceSession.SaveChangesAsync(cancellationToken);
    }

    public static async Task CommitFailureAsync(
        IPersistenceSession persistenceSession,
        IAuditStore auditStore,
        string actor,
        string action,
        string entity,
        string entityId,
        object data,
        string correlationId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        persistenceSession.DiscardChanges();
        await auditStore.RecordAsync(actor, action, entity, entityId, data, correlationId, false, errorMessage, cancellationToken);
        await persistenceSession.SaveChangesAsync(cancellationToken);
    }
}
