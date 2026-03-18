using MediatR;
using RLApp.Application.DTOs;

namespace RLApp.Application.Commands;

/// <summary>
/// Base command for all application commands.
/// Reference: Command Handler Guidelines
/// </summary>
public abstract class Command : IRequest<CommandResult>
{
    public string CorrelationId { get; set; }
    public string UserId { get; set; }

    protected Command(string correlationId, string userId)
    {
        CorrelationId = correlationId;
        UserId = userId;
    }
}

/// <summary>
/// UC-001: Authenticate Staff
/// Command to authenticate a staff member.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class AuthenticateStaffCommand : Command
{
    public string Username { get; set; }
    public string Password { get; set; }

    public AuthenticateStaffCommand(string username, string password, string correlationId)
        : base(correlationId, username)
    {
        Username = username;
        Password = password;
    }
}

/// <summary>
/// UC-002: Manage Internal Roles
/// Command to change staff role.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class ChangeStaffRoleCommand : Command
{
    public string StaffId { get; set; }
    public string NewRole { get; set; }

    public ChangeStaffRoleCommand(string staffId, string newRole, string correlationId, string userId)
        : base(correlationId, userId)
    {
        StaffId = staffId;
        NewRole = newRole;
    }
}

/// <summary>
/// UC-003/UC-004: Consulting Room Lifecycle
/// Command to activate/deactivate consulting room.
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class ActivateConsultingRoomCommand : Command
{
    public string RoomId { get; set; }
    public string RoomName { get; set; }

    public ActivateConsultingRoomCommand(string roomId, string roomName, string correlationId, string userId)
        : base(correlationId, userId)
    {
        RoomId = roomId;
        RoomName = roomName;
    }
}

/// <summary>
/// UC-005: Register Patient Arrival
/// Command to check in a patient.
/// Reference: S-003 Queue Open and Check-in
/// </summary>
public class RegisterPatientArrivalCommand : Command
{
    public string QueueId { get; set; }
    public string PatientId { get; set; }
    public string PatientName { get; set; }

    public RegisterPatientArrivalCommand(string queueId, string patientId, string patientName, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        PatientId = patientId;
        PatientName = patientName;
    }
}

/// <summary>
/// UC-007: Call Next At Cashier
/// Command to call next patient at cashier.
/// Reference: S-004 Cashier Flow
/// </summary>
public class CallNextAtCashierCommand : Command
{
    public string QueueId { get; set; }

    public CallNextAtCashierCommand(string queueId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
    }
}

/// <summary>
/// UC-008: Validate Payment
/// Command to validate patient payment.
/// Reference: S-004 Cashier Flow
/// </summary>
public class ValidatePaymentCommand : Command
{
    public string QueueId { get; set; }
    public string PatientId { get; set; }
    public decimal Amount { get; set; }

    public ValidatePaymentCommand(string queueId, string patientId, decimal amount, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        PatientId = patientId;
        Amount = amount;
    }
}

/// <summary>
/// UC-011: Claim Next Patient For Consultation
/// Command to claim next patient.
/// Reference: S-005 Consultation Flow
/// </summary>
public class ClaimNextPatientCommand : Command
{
    public string QueueId { get; set; }
    public string RoomId { get; set; }

    public ClaimNextPatientCommand(string queueId, string roomId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        RoomId = roomId;
    }
}

/// <summary>
/// UC-012: Call Patient To Consultation
/// Command to call patient for consultation.
/// Reference: S-005 Consultation Flow
/// </summary>
public class CallPatientCommand : Command
{
    public string QueueId { get; set; }
    public string PatientId { get; set; }
    public string RoomId { get; set; }

    public CallPatientCommand(string queueId, string patientId, string roomId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        PatientId = patientId;
        RoomId = roomId;
    }
}

/// <summary>
/// UC-004: Deactivate Consulting Room
/// Command to deactivate consulting room.
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class DeactivateConsultingRoomCommand : Command
{
    public string RoomId { get; set; }

    public DeactivateConsultingRoomCommand(string roomId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        RoomId = roomId;
    }
}

/// <summary>
/// UC-009: Mark Payment Pending
/// Command to mark payment as pending.
/// Reference: S-004 Cashier Flow
/// </summary>
public class MarkPaymentPendingCommand : Command
{
    public string QueueId { get; set; }
    public string PatientId { get; set; }

    public MarkPaymentPendingCommand(string queueId, string patientId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        PatientId = patientId;
    }
}

/// <summary>
/// UC-010: Mark Patient Absence At Cashier
/// Command to mark patient as absent.
/// Reference: S-004 Cashier Flow
/// </summary>
public class MarkAbsenceCommand : Command
{
    public string QueueId { get; set; }
    public string PatientId { get; set; }
    public string RoomId { get; set; }

    public MarkAbsenceCommand(string queueId, string patientId, string roomId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        PatientId = patientId;
        RoomId = roomId;
    }
}

/// <summary>
/// UC-013: Finish Consultation
/// Command to mark consultation as completed.
/// Reference: S-005 Consultation Flow
/// </summary>
public class FinishConsultationCommand : Command
{
    public string QueueId { get; set; }
    public string PatientId { get; set; }
    public string RoomId { get; set; }

    public FinishConsultationCommand(string queueId, string patientId, string roomId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        PatientId = patientId;
        RoomId = roomId;
    }
}

/// <summary>
/// UC-014: Mark Patient Absence At Consultation
/// Command to mark patient as absent during consultation.
/// Reference: S-005 Consultation Flow
/// </summary>
public class MarkAbsenceAtConsultationCommand : Command
{
    public string QueueId { get; set; }
    public string PatientId { get; set; }
    public string RoomId { get; set; }

    public MarkAbsenceAtConsultationCommand(string queueId, string patientId, string roomId, string correlationId, string userId)
        : base(correlationId, userId)
    {
        QueueId = queueId;
        PatientId = patientId;
        RoomId = roomId;
    }
}

/// <summary>
/// UC-016: Rebuild Projections
/// Command to rebuild read model projections from events.
/// Reference: S-008 Event Sourcing and Projections
/// </summary>
public class RebuildProjectionsCommand : Command
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public RebuildProjectionsCommand(DateTime fromDate, DateTime toDate, string correlationId, string userId)
        : base(correlationId, userId)
    {
        FromDate = fromDate;
        ToDate = toDate;
    }
}
