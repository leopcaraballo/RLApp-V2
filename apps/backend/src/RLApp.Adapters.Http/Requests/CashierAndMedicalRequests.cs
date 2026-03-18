using System.ComponentModel.DataAnnotations;

namespace RLApp.Adapters.Http.Requests;

public class CallNextAtCashierRequest
{
    [Required]
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string CashierStationId { get; set; } = string.Empty;
}

public class ValidatePaymentRequest
{
    [Required]
    public string TurnId { get; set; } = string.Empty;

    [Required]
    public string PaymentReference { get; set; } = string.Empty;

    [Required]
    public decimal ValidatedAmount { get; set; }
}

public class MarkPaymentPendingRequest
{
    [Required]
    public string TurnId { get; set; } = string.Empty;

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public int AttemptNumber { get; set; }
}

public class CashierMarkAbsentRequest
{
    [Required]
    public string TurnId { get; set; } = string.Empty;

    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class MedicalCallNextRequest
{
    [Required]
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string ConsultingRoomId { get; set; } = string.Empty;
}

public class StartConsultationRequest
{
    [Required]
    public string TurnId { get; set; } = string.Empty;

    [Required]
    public string ConsultingRoomId { get; set; } = string.Empty;
}

public class FinishConsultationRequest
{
    [Required]
    public string TurnId { get; set; } = string.Empty;

    [Required]
    public string ConsultingRoomId { get; set; } = string.Empty;

    [Required]
    public string Outcome { get; set; } = string.Empty;
}

public class MedicalMarkAbsentRequest
{
    [Required]
    public string TurnId { get; set; } = string.Empty;

    [Required]
    public string ConsultingRoomId { get; set; } = string.Empty;

    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class ActivateConsultingRoomRequest
{
    [Required]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    public string RoomName { get; set; } = string.Empty;
}

public class DeactivateConsultingRoomRequest
{
    [Required]
    public string RoomId { get; set; } = string.Empty;
}
