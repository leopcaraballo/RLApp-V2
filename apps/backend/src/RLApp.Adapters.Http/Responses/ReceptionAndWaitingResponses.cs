namespace RLApp.Adapters.Http.Responses;

public class CheckInResponse
{
    public string QueueId { get; set; } = string.Empty;
    public string TurnId { get; set; } = string.Empty;
    public string TurnNumber { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public int QueuePosition { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public bool IdempotencyReplay { get; set; }
}

public class ReceptionRegisterResponse
{
    public string QueueId { get; set; } = string.Empty;
    public string TurnId { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
