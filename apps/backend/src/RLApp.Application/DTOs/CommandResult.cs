using RLApp.Domain.Common;

namespace RLApp.Application.DTOs;

/// <summary>
/// Result object for successful command execution.
/// Includes operation metadata for traceability.
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsConflict { get; set; }
    public string Message { get; set; }
    public string? ErrorCode { get; set; }
    public string CorrelationId { get; set; }
    public DateTime ExecutedAt { get; set; }

    public static CommandResult Ok(string correlationId, string message = "Operation completed successfully")
        => new() { Success = true, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };

    public static CommandResult Failure(string message, string correlationId)
        => new() { Success = false, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };

    public static CommandResult Failure(DomainException exception, string correlationId)
        => new()
        {
            Success = false,
            Message = exception.Message,
            ErrorCode = exception.Code,
            IsConflict = exception.IsConflict,
            CorrelationId = correlationId,
            ExecutedAt = DateTime.UtcNow
        };

    public static CommandResult NotFound(string message, string correlationId)
        => new() { Success = false, IsNotFound = true, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };
}

/// <summary>
/// Result object with data.
/// </summary>
public class CommandResult<T> : CommandResult
{
    public T Data { get; set; }

    public static CommandResult<T> Ok(T data, string correlationId, string message = "Operation completed successfully")
        => new() { Success = true, Data = data, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };

    public static new CommandResult<T> Failure(string message, string correlationId)
        => new() { Success = false, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };

    public static new CommandResult<T> Failure(DomainException exception, string correlationId)
        => new()
        {
            Success = false,
            Message = exception.Message,
            ErrorCode = exception.Code,
            IsConflict = exception.IsConflict,
            CorrelationId = correlationId,
            ExecutedAt = DateTime.UtcNow
        };

    public static new CommandResult<T> NotFound(string message, string correlationId)
        => new() { Success = false, IsNotFound = true, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };
}
