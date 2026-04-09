namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;

/// <summary>
/// Extended edge-case tests for WaitingQueue that fill gaps
/// identified in the test coverage audit.
/// </summary>
public class WaitingQueueExtendedTests
{
    private const string QueueId = "QUEUE-02";
    private const string QueueName = "Consulta Externa";
    private const string CorrelationId = "corr-ext-test";
    private const string TrajectoryId = "TRJ-Q02-P001-20260401090000000";

    private static WaitingQueue CreateOpenQueue()
    {
        var queue = WaitingQueue.Create(QueueId, QueueName, CorrelationId);
        queue.Open();
        queue.ClearUnraisedEvents();
        return queue;
    }

    private static WaitingQueue CreateQueueWithPatient(string patientId = "PAT-001")
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient(patientId, "Patient A", "APP-001", 0, null, CorrelationId);
        queue.ClearUnraisedEvents();
        return queue;
    }

    // ── CallPatientAtCashier ──────────────────────────────────────

    [Fact]
    public void CallPatientAtCashier_ValidPatient_RaisesCalledAtCashierEvent()
    {
        var queue = CreateQueueWithPatient();
        queue.CallPatientAtCashier("PAT-001", "CAJA-01", CorrelationId);

        var events = queue.GetUnraisedEvents();
        Assert.Contains(events, e => e.EventType == nameof(PatientCalledAtCashier));
    }

    [Fact]
    public void CallPatientAtCashier_UnknownPatient_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        Assert.Throws<DomainException>(() =>
            queue.CallPatientAtCashier("PAT-UNKNOWN", "CAJA-01", CorrelationId));
    }

    // ── MarkPaymentValidated ──────────────────────────────────────

    [Fact]
    public void MarkPaymentValidated_ValidAmount_RaisesPaymentValidatedEvent()
    {
        var queue = CreateQueueWithPatient();
        queue.CallPatientAtCashier("PAT-001", "CAJA-01", CorrelationId);
        queue.ClearUnraisedEvents();

        queue.MarkPaymentValidated("PAT-001", 50000m, "Q02-P001", "PAY-REF", CorrelationId);

        var events = queue.GetUnraisedEvents();
        Assert.Contains(events, e => e.EventType == nameof(PatientPaymentValidated));
    }

    [Fact]
    public void MarkPaymentValidated_ZeroAmount_ThrowsDomainException()
    {
        var queue = CreateQueueWithPatient();
        queue.CallPatientAtCashier("PAT-001", "CAJA-01", CorrelationId);

        Assert.Throws<DomainException>(() =>
            queue.MarkPaymentValidated("PAT-001", 0m, "Q02-P001", "PAY-REF", CorrelationId));
    }

    [Fact]
    public void MarkPaymentValidated_NegativeAmount_ThrowsDomainException()
    {
        var queue = CreateQueueWithPatient();
        queue.CallPatientAtCashier("PAT-001", "CAJA-01", CorrelationId);

        Assert.Throws<DomainException>(() =>
            queue.MarkPaymentValidated("PAT-001", -100m, "Q02-P001", "PAY-REF", CorrelationId));
    }

    // ── MarkPaymentPending ────────────────────────────────────────

    [Fact]
    public void MarkPaymentPending_ValidPatient_RaisesPaymentPendingEvent()
    {
        var queue = CreateQueueWithPatient();
        queue.CallPatientAtCashier("PAT-001", "CAJA-01", CorrelationId);
        queue.ClearUnraisedEvents();

        queue.MarkPaymentPending("PAT-001", CorrelationId);

        var events = queue.GetUnraisedEvents();
        Assert.Contains(events, e => e.EventType == "PatientPaymentPending");
    }

    // ── FIFO ordering guarantee ───────────────────────────────────

    [Fact]
    public void GetNextPatientForCashier_MultiplePatients_ReturnsFIFOOrder()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("PAT-001", "A", "APP-001", 0, null, CorrelationId);
        queue.CheckInPatient("PAT-002", "B", "APP-002", 0, null, CorrelationId);
        queue.CheckInPatient("PAT-003", "C", "APP-003", 0, null, CorrelationId);

        var first = queue.GetNextPatientForCashier();
        Assert.Equal("PAT-001", first);
    }

    // ── CompletePatientAttention guards ───────────────────────────

    [Fact]
    public void CompletePatientAttention_PatientNotInConsultation_ThrowsDomainException()
    {
        var queue = CreateQueueWithPatient();
        Assert.Throws<DomainException>(() =>
            queue.CompletePatientAttention("PAT-001", "ROOM-01", "Q02-P001", "Completed", CorrelationId, TrajectoryId));
    }

    // ── Queue size accuracy ──────────────────────────────────────

    [Fact]
    public void GetQueueSize_AfterMultipleOperations_ReturnsCorrectCount()
    {
        var queue = CreateOpenQueue();
        Assert.Equal(0, queue.GetQueueSize());

        queue.CheckInPatient("PAT-001", "A", "APP-001", 0, null, CorrelationId);
        queue.CheckInPatient("PAT-002", "B", "APP-002", 0, null, CorrelationId);
        Assert.Equal(2, queue.GetQueueSize());

        queue.CallPatientAtCashier("PAT-001", "CAJA-01", CorrelationId);
        queue.MarkPatientAbsentAtCashier("PAT-001", "Q02-P001", "No se presentó", CorrelationId);
        Assert.Equal(1, queue.GetQueueSize());
    }
}
