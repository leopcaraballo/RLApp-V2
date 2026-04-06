namespace RLApp.Domain.Common;

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// Reference: ADR-002 Clean Code and SOLID Baseline
/// </summary>
public class DomainException : Exception
{
    public const string ConcurrencyConflictCode = "CONCURRENCY_CONFLICT";

    public string? Code { get; }

    public bool IsConflict => string.Equals(Code, ConcurrencyConflictCode, StringComparison.Ordinal);

    public DomainException(string message) : base(message) { }

    public DomainException(string message, string? code) : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception inner) : base(message, inner) { }

    public DomainException(string message, string? code, Exception inner) : base(message, inner)
    {
        Code = code;
    }

    public static DomainException ConcurrencyConflict(string aggregateId, int expectedVersion, int? actualVersion = null)
    {
        var message = actualVersion is null
            ? $"Concurrent modification detected for aggregate {aggregateId}. Expected version {expectedVersion} is stale."
            : $"Concurrent modification detected for aggregate {aggregateId}. Expected version {expectedVersion} but found {actualVersion}.";

        return new DomainException(message, ConcurrencyConflictCode);
    }
}
