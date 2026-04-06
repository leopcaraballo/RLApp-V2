namespace RLApp.Domain.Aggregates;

using Common;
using Events;

/// <summary>
/// WaitingQueue Aggregate Root
/// Responsible for patient queue management, room availability and consulting room operations.
/// Reference: Aggregates.md, S-002 through S-005
/// </summary>
public class WaitingQueue : DomainEntity
{
    public string Name { get; private set; }
    public List<string> PatientIds { get; private set; } = new();
    public Dictionary<string, string> PatientRoomAssignments { get; private set; } = new();
    public bool IsOpen { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WaitingQueue(string id, string name) : base(id)
    {
        Name = name;
        IsOpen = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create a new waiting queue.
    /// </summary>
    public static WaitingQueue Create(string id, string name, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Queue ID cannot be empty");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Queue name cannot be empty");

        var queue = new WaitingQueue(id, name);
        queue.RaiseDomainEvent(new WaitingQueueCreated(id, name, correlationId));
        return queue;
    }

    /// <summary>
    /// Open the queue for patient check-ins.
    /// </summary>
    public void Open()
    {
        if (IsOpen)
            throw new DomainException("Queue is already open");

        IsOpen = true;
    }

    /// <summary>
    /// Close the queue.
    /// </summary>
    public void Close()
    {
        if (!IsOpen)
            throw new DomainException("Queue is already closed");

        IsOpen = false;
    }

    /// <summary>
    /// Register a patient arrival (check-in).
    /// </summary>
    public void CheckInPatient(
        string patientId,
        string patientName,
        string? appointmentReference,
        int priority,
        string? notes,
        string correlationId)
    {
        if (!IsOpen)
            throw new DomainException("Queue is not open for check-ins");

        if (PatientIds.Contains(patientId))
            throw new DomainException("Patient is already in the queue");

        PatientIds.Add(patientId);
        RaiseDomainEvent(new PatientCheckedIn(Id, patientId, patientName, appointmentReference, priority, notes, correlationId));
    }

    /// <summary>
    /// Get the next patient in queue.
    /// </summary>
    public string GetNextPatient()
    {
        if (PatientIds.Count == 0)
            throw new DomainException("No patients in queue");

        return PatientIds[0];
    }

    /// <summary>
    /// Assign a patient to a consulting room.
    /// </summary>
    public void AssignPatientToRoom(string patientId, string roomId, string correlationId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        if (PatientRoomAssignments.ContainsKey(patientId))
            throw new DomainException("Patient is already assigned to a room");

        PatientRoomAssignments.Add(patientId, roomId);
        RaiseDomainEvent(new PatientClaimedForAttention(Id, patientId, roomId, correlationId));
    }

    /// <summary>
    /// Call a patient for consultation.
    /// </summary>
    public void CallPatient(string patientId, string roomId, string correlationId, string trajectoryId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        if (string.IsNullOrWhiteSpace(trajectoryId))
            throw new DomainException("Trajectory ID cannot be empty");

        RaiseDomainEvent(new PatientCalled(Id, patientId, roomId, correlationId, trajectoryId));
    }

    /// <summary>
    /// Call a patient for cashier flow.
    /// </summary>
    public void CallPatientAtCashier(string patientId, string? cashierStationId, string correlationId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        RaiseDomainEvent(new PatientCalledAtCashier(Id, patientId, cashierStationId, correlationId));
    }

    /// <summary>
    /// Register a successful payment validation for a patient still in the operational flow.
    /// </summary>
    public void MarkPaymentValidated(string patientId, decimal amount, string? turnId, string? paymentReference, string correlationId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero");

        RaiseDomainEvent(new PatientPaymentValidated(Id, patientId, amount, turnId, paymentReference, correlationId));
    }

    /// <summary>
    /// Mark payment as pending while preserving the patient in the operational flow.
    /// </summary>
    public void MarkPaymentPending(string patientId, string correlationId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        RaiseDomainEvent(new PatientPaymentPending(Id, patientId, correlationId));
    }

    /// <summary>
    /// Mark patient attention as completed.
    /// </summary>
    public void CompletePatientAttention(string patientId, string roomId, string? turnId, string? outcome, string correlationId, string trajectoryId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        if (string.IsNullOrWhiteSpace(trajectoryId))
            throw new DomainException("Trajectory ID cannot be empty");

        PatientIds.Remove(patientId);
        PatientRoomAssignments.Remove(patientId);
        RaiseDomainEvent(new PatientAttentionCompleted(Id, patientId, roomId, turnId, outcome, correlationId, trajectoryId));
    }

    /// <summary>
    /// Mark patient as absent.
    /// </summary>
    public void MarkPatientAbsent(string patientId, string? turnId, string? reason, string correlationId, string trajectoryId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        if (string.IsNullOrWhiteSpace(trajectoryId))
            throw new DomainException("Trajectory ID cannot be empty");

        PatientIds.Remove(patientId);
        PatientRoomAssignments.Remove(patientId);
        RaiseDomainEvent(new PatientAbsentAtConsultation(Id, patientId, turnId, reason, correlationId, trajectoryId));
    }

    /// <summary>
    /// Mark patient as absent at cashier and remove them from the queue.
    /// </summary>
    public void MarkPatientAbsentAtCashier(string patientId, string? turnId, string? reason, string correlationId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        PatientIds.Remove(patientId);
        PatientRoomAssignments.Remove(patientId);
        RaiseDomainEvent(new PatientAbsentAtCashier(Id, patientId, turnId, reason, correlationId));
    }

    /// <summary>
    /// Cancel patient by absence policy.
    /// </summary>
    public void CancelPatientByAbsence(string patientId, string correlationId)
    {
        if (!PatientIds.Contains(patientId))
            throw new DomainException("Patient is not in the queue");

        PatientIds.Remove(patientId);
        PatientRoomAssignments.Remove(patientId);
        RaiseDomainEvent(new PatientCancelledByAbsence(Id, patientId, correlationId));
    }

    /// <summary>
    /// Get the number of patients in the queue.
    /// </summary>
    public int GetQueueSize() => PatientIds.Count;
}
