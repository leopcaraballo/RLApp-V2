using System.ComponentModel.DataAnnotations;

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
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string RoomId { get; set; } = string.Empty;
}

public class ClaimNextPatientRequest
{
    [Required]
    public string QueueId { get; set; } = string.Empty;

    [Required]
    public string RoomId { get; set; } = string.Empty;
}
