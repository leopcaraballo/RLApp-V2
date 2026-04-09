namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;

/// <summary>
/// Unit tests for the ConsultingRoom aggregate.
/// Validates room lifecycle: create, activate, deactivate, patient assignment.
/// </summary>
public class ConsultingRoomTests
{
    private const string CorrelationId = "corr-test";
    private const string TrajectoryId = "TRJ-Q-1-P-1-20260405090000000";

    private static ConsultingRoom CreateActiveRoom(string id = "room-1", string name = "Consulting Room A")
    {
        var room = ConsultingRoom.Create(id, name, CorrelationId);
        room.ClearUnraisedEvents();
        return room;
    }

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    [Fact]
    public void Create_ValidInput_RaisesConsultingRoomActivatedEvent()
    {
        var room = ConsultingRoom.Create("room-1", "Room A", CorrelationId);

        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<ConsultingRoomActivated>(events[0]);
        Assert.True(room.IsActive);
        Assert.Equal("room-1", room.Id);
        Assert.Equal("Room A", room.RoomName);
    }

    [Fact]
    public void Create_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ConsultingRoom.Create("", "Room A", CorrelationId));
    }

    [Fact]
    public void Create_EmptyName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ConsultingRoom.Create("room-1", "", CorrelationId));
    }

    // -------------------------------------------------------------------------
    // Activate
    // -------------------------------------------------------------------------

    [Fact]
    public void Activate_AlreadyActiveRoom_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        Assert.Throws<DomainException>(() => room.Activate(CorrelationId));
    }

    // -------------------------------------------------------------------------
    // Deactivate
    // -------------------------------------------------------------------------

    [Fact]
    public void Deactivate_ActiveRoom_RaisesConsultingRoomDeactivatedEvent()
    {
        var room = CreateActiveRoom();
        room.Deactivate(CorrelationId);

        Assert.False(room.IsActive);
        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<ConsultingRoomDeactivated>(events[0]);
    }

    [Fact]
    public void Deactivate_InactiveRoom_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.Deactivate(CorrelationId);
        room.ClearUnraisedEvents();

        Assert.Throws<DomainException>(() => room.Deactivate(CorrelationId));
    }

    [Fact]
    public void Deactivate_RoomWithCurrentPatient_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("p-1", "dr-1", CorrelationId);
        room.ClearUnraisedEvents();

        Assert.Throws<DomainException>(() => room.Deactivate(CorrelationId));
    }

    // -------------------------------------------------------------------------
    // AssignPatient
    // -------------------------------------------------------------------------

    [Fact]
    public void AssignPatient_ActiveRoom_AssignsPatientAndRaisesEvent()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("p-1", "dr-1", CorrelationId);

        Assert.Equal("p-1", room.CurrentPatientId);
        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PatientClaimedForAttention>(events[0]);
        Assert.Equal(PatientClaimedForAttention.ClaimedPhase, @event.ConsultationPhase);
    }

    [Fact]
    public void AssignPatient_InactiveRoom_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.Deactivate(CorrelationId);
        room.ClearUnraisedEvents();

        Assert.Throws<DomainException>(() => room.AssignPatient("p-1", "dr-1", CorrelationId));
    }

    [Fact]
    public void AssignPatient_RoomAlreadyOccupied_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("p-1", "dr-1", CorrelationId);
        room.ClearUnraisedEvents();

        Assert.Throws<DomainException>(() => room.AssignPatient("p-2", "dr-1", CorrelationId));
    }

    // -------------------------------------------------------------------------
    // CompleteAttention
    // -------------------------------------------------------------------------

    [Fact]
    public void CompleteAttention_WithPatient_ClearsPatientAndRaisesEvent()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("p-1", "dr-1", CorrelationId);
        room.ClearUnraisedEvents();

        room.CompleteAttention(null, null, CorrelationId, TrajectoryId);

        Assert.Null(room.CurrentPatientId);
        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PatientAttentionCompleted>(events[0]);
        Assert.Equal(TrajectoryId, @event.TrajectoryId);
    }

    [Fact]
    public void CompleteAttention_NoCurrentPatient_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        Assert.Throws<DomainException>(() => room.CompleteAttention(null, null, CorrelationId, TrajectoryId));
    }

    // -------------------------------------------------------------------------
    // MarkPatientAbsent
    // -------------------------------------------------------------------------

    [Fact]
    public void MarkPatientAbsent_WithPatient_ClearsPatientAndRaisesEvent()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("p-1", "dr-1", CorrelationId);
        room.ClearUnraisedEvents();

        room.MarkPatientAbsent(null, null, CorrelationId, TrajectoryId);

        Assert.Null(room.CurrentPatientId);
        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PatientAbsentAtConsultation>(events[0]);
        Assert.Equal(TrajectoryId, @event.TrajectoryId);
    }

    [Fact]
    public void MarkPatientAbsent_NoCurrentPatient_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        Assert.Throws<DomainException>(() => room.MarkPatientAbsent(null, null, CorrelationId, TrajectoryId));
    }
}
