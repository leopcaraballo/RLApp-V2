using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

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
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

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
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

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
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("consultingRoomId")]
    public string ConsultingRoomId { get; set; } = string.Empty;

    [Required]
    public string Outcome { get; set; } = string.Empty;
}

public class MedicalMarkAbsentRequest
{
    [Required]
    [JsonPropertyName("turnId")]
    public string TurnId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("consultingRoomId")]
    public string ConsultingRoomId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class ActivateConsultingRoomRequest
{
    [Required]
    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("roomName")]
    public string RoomName { get; set; } = string.Empty;
}

public class DeactivateConsultingRoomRequest
{
    [Required]
    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;
}
