namespace RLApp.Adapters.Http.Responses;

public class CallNextAtCashierResponse
{
    public string TurnId { get; set; } = string.Empty;
    public string TurnNumber { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string CashierStationId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class ValidatePaymentResponse
{
    public string TurnId { get; set; } = string.Empty;
    public string PreviousState { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class MarkPaymentPendingResponse
{
    public string TurnId { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class CashierMarkAbsentResponse
{
    public string TurnId { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class MedicalCallNextResponse
{
    public string TurnId { get; set; } = string.Empty;
    public string TurnNumber { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string ConsultingRoomId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class FinishConsultationResponse
{
    public string TurnId { get; set; } = string.Empty;
    public string PreviousState { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public DateTime FinishedAt { get; set; }
    public bool ConsultingRoomReleased { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class MedicalMarkAbsentResponse
{
    public string TurnId { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string PolicyOutcome { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
