namespace RLApp.Domain.Events;

using Common;
using System.Text.Json.Serialization;

/// <summary>
/// EV-001 WaitingQueueCreated
/// Raised when a new waiting queue is created.
/// </summary>
public class WaitingQueueCreated : DomainEvent
{
    [JsonPropertyName("queueName")]
    public string QueueName { get; set; } = string.Empty;

    public WaitingQueueCreated(string aggregateId, string queueName, string correlationId)
        : base(nameof(WaitingQueueCreated), aggregateId, correlationId)
    {
        QueueName = queueName;
    }

    protected WaitingQueueCreated() { }
}

/// <summary>
/// EV-002 PatientCheckedIn
/// Raised when a patient checks in and joins the queue.
/// </summary>
public class PatientCheckedIn : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("patientName")]
    public string PatientName { get; set; } = string.Empty;

    [JsonPropertyName("appointmentReference")]
    public string? AppointmentReference { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    public PatientCheckedIn(
        string aggregateId,
        string patientId,
        string patientName,
        string? appointmentReference,
        int priority,
        string? notes,
        string correlationId)
        : base(nameof(PatientCheckedIn), aggregateId, correlationId)
    {
        PatientId = patientId;
        PatientName = patientName;
        AppointmentReference = appointmentReference;
        Priority = priority;
        Notes = notes;
    }

    protected PatientCheckedIn() { }
}

/// <summary>
/// EV-003 PatientCalledAtCashier
/// Raised when a patient is called at the cashier.
/// </summary>
public class PatientCalledAtCashier : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("cashierStationId")]
    public string? CashierStationId { get; set; }

    public PatientCalledAtCashier(string aggregateId, string patientId, string? cashierStationId, string correlationId)
        : base(nameof(PatientCalledAtCashier), aggregateId, correlationId)
    {
        PatientId = patientId;
        CashierStationId = cashierStationId;
    }

    protected PatientCalledAtCashier() { }
}

/// <summary>
/// EV-004 PatientPaymentValidated
/// Raised when patient payment is validated successfully.
/// </summary>
public class PatientPaymentValidated : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    [JsonPropertyName("paymentReference")]
    public string? PaymentReference { get; set; }

    public PatientPaymentValidated(
        string aggregateId,
        string patientId,
        decimal amount,
        string? turnId,
        string? paymentReference,
        string correlationId)
        : base(nameof(PatientPaymentValidated), aggregateId, correlationId)
    {
        PatientId = patientId;
        Amount = amount;
        TurnId = turnId;
        PaymentReference = paymentReference;
    }

    protected PatientPaymentValidated() { }
}

/// <summary>
/// EV-005 PatientPaymentPending
/// Raised when patient payment is marked as pending.
/// </summary>
public class PatientPaymentPending : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    public PatientPaymentPending(string aggregateId, string patientId, string correlationId)
        : base(nameof(PatientPaymentPending), aggregateId, correlationId)
    {
        PatientId = patientId;
    }

    protected PatientPaymentPending() { }
}

/// <summary>
/// EV-006 PatientAbsentAtCashier
/// Raised when a patient is absent at cashier.
/// </summary>
public class PatientAbsentAtCashier : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    public PatientAbsentAtCashier(string aggregateId, string patientId, string? turnId, string? reason, string correlationId)
        : base(nameof(PatientAbsentAtCashier), aggregateId, correlationId)
    {
        PatientId = patientId;
        TurnId = turnId;
        Reason = reason;
    }

    protected PatientAbsentAtCashier() { }
}

/// <summary>
/// EV-007 PatientCancelledByPayment
/// Raised when a patient is cancelled due to payment policy.
/// </summary>
public class PatientCancelledByPayment : DomainEvent
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    public PatientCancelledByPayment(string aggregateId, string patientId, string correlationId)
        : base(nameof(PatientCancelledByPayment), aggregateId, correlationId)
    {
        PatientId = patientId;
    }

    protected PatientCancelledByPayment() { }
}
