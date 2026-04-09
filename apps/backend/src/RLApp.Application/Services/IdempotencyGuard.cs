namespace RLApp.Application.Services;

using System.Collections.Concurrent;

/// <summary>
/// In-process idempotency guard that prevents duplicate command execution
/// during the lifetime of the application instance.
/// Uses CorrelationId+IdempotencyKey as the deduplication key.
/// </summary>
public sealed class IdempotencyGuard
{
    private readonly ConcurrentDictionary<string, byte> _activeKeys = new(StringComparer.Ordinal);

    /// <summary>
    /// Tries to acquire an idempotency lock for the given key.
    /// Returns true if the key was acquired (first execution).
    /// Returns false if the key is already active (duplicate).
    /// If idempotencyKey is null/empty, the guard is skipped (always returns true).
    /// </summary>
    public bool TryAcquire(string? idempotencyKey, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return true;

        var compositeKey = $"{correlationId}|{idempotencyKey}";
        return _activeKeys.TryAdd(compositeKey, 0);
    }

    /// <summary>
    /// Releases the idempotency lock so future retries with different correlationIds can proceed.
    /// </summary>
    public void Release(string? idempotencyKey, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return;

        var compositeKey = $"{correlationId}|{idempotencyKey}";
        _activeKeys.TryRemove(compositeKey, out _);
    }
}
