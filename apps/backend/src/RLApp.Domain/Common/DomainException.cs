namespace RLApp.Domain.Common;

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// Reference: ADR-002 Clean Code and SOLID Baseline
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
