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
    // GetNextPatient
    // -------------------------------------------------------------------------

    [Fact]
    public void GetNextPatient_WithPatients_ReturnsFirstPatient()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
        queue.CheckInPatient("p-2", "Bob", null, 1, null, CorrelationId);

        Assert.Equal("p-1", queue.GetNextPatient());
    }

    [Fact]
    public void GetNextPatient_EmptyQueue_ThrowsDomainException()
    {
        var queue = CreateOpenQueue();
        Assert.Throws<DomainException>(() => queue.GetNextPatient());
    }

    // -------------------------------------------------------------------------
    // MarkPatientAbsent
    // -------------------------------------------------------------------------

    [Fact]
    public void MarkPatientAbsent_ExistingPatient_RemovesFromQueue()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
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

    // -------------------------------------------------------------------------
    // CompletePatientAttention
    // -------------------------------------------------------------------------

    [Fact]
    public void CompletePatientAttention_ExistingPatient_RemovesFromQueue()
    {
        var queue = CreateOpenQueue();
        queue.CheckInPatient("p-1", "Alice", null, 1, null, CorrelationId);
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
        queue.ClearUnraisedEvents();

        queue.CallPatient("p-1", "room-1", CorrelationId, TrajectoryId);

        var events = queue.GetUnraisedEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PatientCalled>(events[0]);
        Assert.Equal(TrajectoryId, @event.TrajectoryId);
    }
}
