namespace RLApp.Domain.Aggregates;

using Common;
using Events;

/// <summary>
/// ConsultingRoom Aggregate Root
/// Manages the lifecycle of a consulting room: activation, deactivation, and patient attention workflow.
/// Reference: S-002 Consulting Room Lifecycle, UC-003, UC-005, UC-006
/// Implements: ADR-001 (Hexagonal), ADR-003 (CQRS), Event Sourcing
/// </summary>
public class ConsultingRoom : DomainEntity
{
    public string RoomName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public string? CurrentPatientId { get; private set; }
    public string? CurrentConsultantId { get; private set; }

    private ConsultingRoom(string id, string roomName) : base(id)
    {
        RoomName = roomName;
        IsActive = false;
        ActivatedAt = null;
        DeactivatedAt = null;
        CurrentPatientId = null;
        CurrentConsultantId = null;
    }

    /// <summary>
    /// Create and activate a new consulting room.
    /// Reference: UC-003
    /// </summary>
    public static ConsultingRoom Create(string id, string roomName, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Room ID cannot be empty");
        if (string.IsNullOrWhiteSpace(roomName))
            throw new DomainException("Room name cannot be empty");

        var room = new ConsultingRoom(id, roomName);
        room.Activate(correlationId);
        return room;
    }

    /// <summary>
    /// Activate the consulting room for daily operations.
    /// Raises: ConsultingRoomActivated
    /// Reference: UC-003
    /// </summary>
    public void Activate(string correlationId)
    {
        if (IsActive)
            throw new DomainException("Consulting room is already active");

        IsActive = true;
        ActivatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ConsultingRoomActivated(Id, RoomName, correlationId));
    }

    /// <summary>
    /// Deactivate the consulting room.
    /// Raises: ConsultingRoomDeactivated
    /// Reference: UC-003
    /// </summary>
    public void Deactivate(string correlationId)
    {
        if (!IsActive)
            throw new DomainException("Consulting room is not active");

        if (CurrentPatientId != null)
            throw new DomainException("Cannot deactivate room while a patient is being attended");

        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ConsultingRoomDeactivated(Id, correlationId));
    }

    /// <summary>
    /// Assign a patient for consultation in this room.
    /// Raises: PatientClaimedForAttention
    /// Reference: UC-005
    /// </summary>
    public void AssignPatient(string patientId, string consultantId, string correlationId)
    {
        if (!IsActive)
            throw new DomainException("Consulting room is not active");

        if (CurrentPatientId != null)
            throw new DomainException("Room is already occupied with a patient");

        CurrentPatientId = patientId;
        CurrentConsultantId = consultantId;
        RaiseDomainEvent(new PatientClaimedForAttention(
            Id,
            patientId,
            Id,
            correlationId,
            consultationPhase: PatientClaimedForAttention.ClaimedPhase));
    }

    /// <summary>
    /// Call patient for consultation (from waiting room to consulting room).
    /// Raises: PatientCalled
    /// Reference: UC-006
    /// </summary>
    public void CallPatient(string patientId, string roomId, string correlationId, string trajectoryId)
    {
        if (!IsActive)
            throw new DomainException("Consulting room is not active");

        if (CurrentPatientId != patientId)
            throw new DomainException($"Patient {patientId} is not assigned to this room");

        if (string.IsNullOrWhiteSpace(trajectoryId))
            throw new DomainException("Trajectory ID cannot be empty");

        RaiseDomainEvent(new PatientCalled(Id, patientId, roomId ?? Id, correlationId, trajectoryId));
    }

    /// <summary>
    /// Complete patient attention in this room.
    /// Clears the room for the next patient.
    /// Raises: PatientAttentionCompleted
    /// Reference: UC-006
    /// </summary>
    public void CompleteAttention(string? turnId, string? outcome, string correlationId, string trajectoryId)
    {
        if (CurrentPatientId == null)
            throw new DomainException("No patient is currently being attended");

        if (string.IsNullOrWhiteSpace(trajectoryId))
            throw new DomainException("Trajectory ID cannot be empty");

        var patientId = CurrentPatientId;
        CurrentPatientId = null;
        CurrentConsultantId = null;

        RaiseDomainEvent(new PatientAttentionCompleted(Id, patientId, Id, turnId, outcome, correlationId, trajectoryId));
    }

    /// <summary>
    /// Mark patient as absent during consultation.
    /// Clears the room for the next patient.
    /// Raises: PatientAbsentAtConsultation
    /// Reference: UC-006
    /// </summary>
    public void MarkPatientAbsent(string? turnId, string? reason, string correlationId, string trajectoryId)
    {
        if (CurrentPatientId == null)
            throw new DomainException("No patient is currently being attended");

        if (string.IsNullOrWhiteSpace(trajectoryId))
            throw new DomainException("Trajectory ID cannot be empty");

        var patientId = CurrentPatientId;
        CurrentPatientId = null;
        CurrentConsultantId = null;

        RaiseDomainEvent(new PatientAbsentAtConsultation(Id, patientId, turnId, reason, correlationId, trajectoryId));
    }
}
