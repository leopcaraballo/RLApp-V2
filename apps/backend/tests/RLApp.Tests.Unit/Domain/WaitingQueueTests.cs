namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;

/// <summary>
/// Unit tests for the WaitingQueue aggregate.
/// Validates patient lifecycle: create, check-in, call, complete, absent.
/// </summary>
public class WaitingQueueTests
{
    private const string CorrelationId = "corr-test";
    private const string TrajectoryId = "TRJ-Q-1-P-1-20260405090000000";

    private static WaitingQueue CreateOpenQueue(string id = "q-1", string name = "Queue A")
    {
        var queue = WaitingQueue.Create(id, name, CorrelationId);
        queue.Open();
        queue.ClearUnraisedEvents();
        return queue;
    }

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    [Fact]
    public void Create_ValidInput_RaisesWaitingQueueCreatedEvent()
    {
        var queue = WaitingQueue.Create("q-1", "Queue A", CorrelationId);

        var events = queue.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<WaitingQueueCreated>(events[0]);
        Assert.False(queue.IsOpen);
    }

    [Fact]
    public void Create_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => WaitingQueue.Create("", "Queue A", CorrelationId));
    }

    [Fact]
    public void Create_EmptyName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => WaitingQueue.Create("q-1", "", CorrelationId));
    }

    // -------------------------------------------------------------------------
    // Open / Close
    // -------------------------------------------------------------------------

    [Fact]
    public void Open_ClosedQueue_SetsIsOpenTrue()
    {
        var queue = WaitingQueue.Create("q-1", "Queue A", CorrelationId);
        queue.Open();
        Assert.True(queue.IsOpen);
    }

    [Fact]
    public void Open_AlreadyOpen_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        Assert.Throws<DomainException>(() => queue.Open());
    }

    [Fact]
    public void Close_OpenQueue_SetsIsOpenFalse()
    {
        var queue = CreateOpenQueue();
        queue.Close();
        Assert.False(queue.IsOpen);
    }

    [Fact]
    public void Close_AlreadyClosed_ThrowsDomainException()
    {
        var queue = WaitingQueue.Create("q-1", "Queue A", CorrelationId);
        Assert.Throws<DomainException>(() => queue.Close());
    }

    // -------------------------------------------------------------------------
    // CheckInPatient
    // -------------------------------------------------------------------------

    [Fact]
    public void CheckInPatient_ValidPatient_AddsToQueueAndRaisesEvent()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);

        Assert.Equal(1, queue.GetQueueSize());
        var events = queue.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<PatientCheckedIn>(events[0]);
    }

    [Fact]
    public void CheckInPatient_QueueClosed_ThrowsDomainException()
    {
        var queue = WaitingQueue.Create("q-1", "Queue A", CorrelationId);
        Assert.Throws<DomainException>(() => queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId));
    }

    [Fact]
    public void CheckInPatient_DuplicatePatient_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);

        Assert.Throws<DomainException>(() => queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId));
    }

    // -------------------------------------------------------------------------
    // GetNextPatientForCashier / GetNextPatientForConsultation
    // -------------------------------------------------------------------------

    [Fact]
    public void GetNextPatientForCashier_WithPatients_ReturnsFirstPatient()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        queue.CheckInPatient("p-2", "Bob", null, 1, null, CorrelationId);

        Assert.Equal("p-1", queue.GetNextPatientForCashier());
    }

    [Fact]
    public void GetNextPatientForCashier_EmptyQueue_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        Assert.Throws<DomainException>(() => queue.GetNextPatientForCashier());
    }

    [Fact]
    public void GetNextPatientForConsultation_WhenPaymentValidated_ReturnsEligiblePatient()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        queue.CallPatientAtCashier("p-1", "cash-1", CorrelationId);
        queue.MarkPaymentValidated("p-1", 25m, "q-1-p-1", "PAY-001", CorrelationId);

        Assert.Equal("p-1", queue.GetNextPatientForConsultation());
    }

    [Fact]
    public void GetNextPatientForConsultation_WithoutEligiblePatient_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);

        Assert.Throws<DomainException>(() => queue.GetNextPatientForConsultation());
    }

    [Fact]
    public void AssignPatientToRoom_BeforePaymentValidated_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);

        Assert.Throws<DomainException>(() => queue.AssignPatientToRoom("p-1", "room-1", CorrelationId));
    }

    // -------------------------------------------------------------------------
    // MarkPatientAbsent
    // -------------------------------------------------------------------------

    [Fact]
    public void MarkPatientAbsent_ExistingPatient_RemovesFromQueue()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        PreparePatientForConsultation(queue);
        queue.AssignPatientToRoom("p-1", "room-1", CorrelationId);
        queue.CallPatient("p-1", "room-1", CorrelationId, TrajectoryId);
        queue.ClearUnraisedEvents();

        queue.MarkPatientAbsent("p-1", null, null, CorrelationId, TrajectoryId);

        Assert.Equal(0, queue.GetQueueSize());
        var events = queue.GetUnraisedEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PatientAbsentAtConsultation>(events[0]);
        Assert.Equal(TrajectoryId, @event.TrajectoryId);
    }

    [Fact]
    public void MarkPatientAbsent_UnknownPatient_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        Assert.Throws<DomainException>(() => queue.MarkPatientAbsent("unknown", null, null, CorrelationId, TrajectoryId));
    }

    [Fact]
    public void MarkPatientAbsentAtCashier_ExistingPatient_RemovesFromQueueAndRaisesEvent()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        queue.ClearUnraisedEvents();

        queue.MarkPatientAbsentAtCashier("p-1", null, "cashier-no-show", CorrelationId);

        Assert.Equal(0, queue.GetQueueSize());
        var events = queue.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<PatientAbsentAtCashier>(events[0]);
    }

    [Fact]
    public void MarkPatientAbsentAtCashier_UnknownPatient_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        Assert.Throws<DomainException>(() => queue.MarkPatientAbsentAtCashier("unknown", null, null, CorrelationId));
    }

    // -------------------------------------------------------------------------
    // CompletePatientAttention
    // -------------------------------------------------------------------------

    [Fact]
    public void CompletePatientAttention_ExistingPatient_RemovesFromQueue()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        PreparePatientForConsultation(queue);
        queue.AssignPatientToRoom("p-1", "room-1", CorrelationId);
        queue.CallPatient("p-1", "room-1", CorrelationId, TrajectoryId);
        queue.StartPatientAttention("p-1", "room-1", CorrelationId, TrajectoryId);
        queue.ClearUnraisedEvents();

        queue.CompletePatientAttention("p-1", "room-1", null, null, CorrelationId, TrajectoryId);

        Assert.Equal(0, queue.GetQueueSize());
        var events = queue.GetUnraisedEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PatientAttentionCompleted>(events[0]);
        Assert.Equal(TrajectoryId, @event.TrajectoryId);
    }

    [Fact]
    public void CallPatient_ExistingPatient_RaisesEventWithTrajectoryId()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        PreparePatientForConsultation(queue);
        queue.AssignPatientToRoom("p-1", "room-1", CorrelationId);
        queue.ClearUnraisedEvents();

        queue.CallPatient("p-1", "room-1", CorrelationId, TrajectoryId);

        var events = queue.GetUnraisedEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PatientCalled>(events[0]);
        Assert.Equal(TrajectoryId, @event.TrajectoryId);
    }

    [Fact]
    public void AssignPatientToRoom_RaisesClaimedPhaseEvent()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        PreparePatientForConsultation(queue);
        queue.ClearUnraisedEvents();

        queue.AssignPatientToRoom("p-1", "room-1", CorrelationId);

        var @event = Assert.IsType<PatientClaimedForAttention>(Assert.Single(queue.GetUnraisedEvents()));
        Assert.Equal(PatientClaimedForAttention.ClaimedPhase, @event.ConsultationPhase);
        Assert.False(@event.RepresentsStartedAttention);
    }

    [Fact]
    public void StartPatientAttention_AfterCall_RaisesStartedPhaseEvent()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        PreparePatientForConsultation(queue);
        queue.AssignPatientToRoom("p-1", "room-1", CorrelationId);
        queue.CallPatient("p-1", "room-1", CorrelationId, TrajectoryId);
        queue.ClearUnraisedEvents();

        queue.StartPatientAttention("p-1", "room-1", CorrelationId, TrajectoryId);

        var @event = Assert.IsType<PatientClaimedForAttention>(Assert.Single(queue.GetUnraisedEvents()));
        Assert.Equal(PatientClaimedForAttention.StartedPhase, @event.ConsultationPhase);
        Assert.True(@event.RepresentsStartedAttention);
        Assert.Equal(TrajectoryId, @event.TrajectoryId);
    }

    [Fact]
    public void StartPatientAttention_BeforeCall_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        PreparePatientForConsultation(queue);
        queue.AssignPatientToRoom("p-1", "room-1", CorrelationId);

        Assert.Throws<DomainException>(() => queue.StartPatientAttention("p-1", "room-1", CorrelationId, TrajectoryId));
    }

    private static void PreparePatientForConsultation(WaitingQueue queue, string patientId = "p-1")
    {
        queue.CallPatientAtCashier(patientId, "cash-1", CorrelationId);
        queue.MarkPaymentValidated(patientId, 25m, $"{queue.Id}-{patientId}", "PAY-001", CorrelationId);
    }
}
