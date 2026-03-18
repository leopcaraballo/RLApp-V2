namespace RLApp.Domain.Events;

using Common;

/// <summary>
/// EV-001 WaitingQueueCreated
/// Raised when a new waiting queue is created.
/// </summary>
public class WaitingQueueCreated : DomainEvent
{
    public string QueueName { get; }

    public WaitingQueueCreated(string queueId, string queueName, string correlationId)
        : base(nameof(WaitingQueueCreated), queueId, correlationId)
    {
        QueueName = queueName;
    }
}

/// <summary>
/// EV-002 PatientCheckedIn
/// Raised when a patient checks in and joins the queue.
/// </summary>
public class PatientCheckedIn : DomainEvent
{
    public string PatientId { get; }
    public string PatientName { get; }

    public PatientCheckedIn(string queueId, string patientId, string patientName, string correlationId)
        : base(nameof(PatientCheckedIn), queueId, correlationId)
    {
        PatientId = patientId;
        PatientName = patientName;
    }
}

/// <summary>
/// EV-003 PatientCalledAtCashier
/// Raised when a patient is called at the cashier.
/// </summary>
public class PatientCalledAtCashier : DomainEvent
{
    public string PatientId { get; }

    public PatientCalledAtCashier(string queueId, string patientId, string correlationId)
        : base(nameof(PatientCalledAtCashier), queueId, correlationId)
    {
        PatientId = patientId;
    }
}

/// <summary>
/// EV-004 PatientPaymentValidated
/// Raised when patient payment is validated successfully.
/// </summary>
public class PatientPaymentValidated : DomainEvent
{
    public string PatientId { get; }
    public decimal Amount { get; }

    public PatientPaymentValidated(string queueId, string patientId, decimal amount, string correlationId)
        : base(nameof(PatientPaymentValidated), queueId, correlationId)
    {
        PatientId = patientId;
        Amount = amount;
    }
}

/// <summary>
/// EV-005 PatientPaymentPending
/// Raised when patient payment is marked as pending.
/// </summary>
public class PatientPaymentPending : DomainEvent
{
    public string PatientId { get; }

    public PatientPaymentPending(string queueId, string patientId, string correlationId)
        : base(nameof(PatientPaymentPending), queueId, correlationId)
    {
        PatientId = patientId;
    }
}

/// <summary>
/// EV-006 PatientAbsentAtCashier
/// Raised when a patient is absent at cashier.
/// </summary>
public class PatientAbsentAtCashier : DomainEvent
{
    public string PatientId { get; }

    public PatientAbsentAtCashier(string queueId, string patientId, string correlationId)
        : base(nameof(PatientAbsentAtCashier), queueId, correlationId)
    {
        PatientId = patientId;
    }
}

/// <summary>
/// EV-007 PatientCancelledByPayment
/// Raised when a patient is cancelled due to payment policy.
/// </summary>
public class PatientCancelledByPayment : DomainEvent
{
    public string PatientId { get; }

    public PatientCancelledByPayment(string queueId, string patientId, string correlationId)
        : base(nameof(PatientCancelledByPayment), queueId, correlationId)
    {
        PatientId = patientId;
    }
}
