namespace RLApp.Application.DTOs;

/// <summary>
/// Result payload for cashier call-next operations.
/// </summary>
public class PatientCallResultDto
{
    public string PatientId { get; set; } = string.Empty;
    public DateTime CalledAt { get; set; }
    public int QueuePosition { get; set; }
}

/// <summary>
/// Result payload for consultation claim operations.
/// </summary>
public class ClaimedPatientResultDto
{
    public string QueueId { get; set; } = string.Empty;
    public string TurnId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime ClaimedAt { get; set; }
}

/// <summary>
/// Result payload for patient registration.
/// </summary>
public class RegisterPatientResultDto
{
    public string QueueId { get; set; } = string.Empty;
    public string TurnId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}
