using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RLApp.Adapters.Http.Requests;

public class CheckInRequest
{
    [Required]
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string AppointmentReference { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    public string? PatientName { get; set; }

    [Required]
    public string ConsultationType { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class ReceptionRegisterRequest
{
    [Required]
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    public string? PatientName { get; set; }

    [Required]
    public string AppointmentReference { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class CallPatientRequest
{
    [Required]
    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    [JsonPropertyName("patientId")]
    public string? PatientId { get; set; }

    [JsonPropertyName("consultingRoomId")]
    public string? ConsultingRoomId { get; set; }

    [JsonPropertyName("roomId")]
    public string? LegacyRoomId { get; set; }

    public string ResolveConsultingRoomId()
        => string.IsNullOrWhiteSpace(ConsultingRoomId) ? LegacyRoomId ?? string.Empty : ConsultingRoomId;
}

public class ClaimNextPatientRequest
{
    [Required]
    [JsonPropertyName("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [JsonPropertyName("consultingRoomId")]
    public string? ConsultingRoomId { get; set; }

    [JsonPropertyName("roomId")]
    public string? LegacyRoomId { get; set; }

    public string ResolveConsultingRoomId()
        => string.IsNullOrWhiteSpace(ConsultingRoomId) ? LegacyRoomId ?? string.Empty : ConsultingRoomId;
}
