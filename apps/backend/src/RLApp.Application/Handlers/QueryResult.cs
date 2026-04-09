namespace RLApp.Application.Handlers;

/// <summary>
/// Result object for query execution.
/// </summary>
public class QueryResult<T> where T : class
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public T Data { get; set; } = default!;
    public DateTime ExecutedAt { get; set; }

    public static QueryResult<T> Ok(T data, string correlationId, string message = "Query executed successfully")
        => new() { Success = true, Data = data, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };

    public static QueryResult<T> Failure(string message, string correlationId)
        => new() { Success = false, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };
}
