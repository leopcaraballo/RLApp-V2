namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;

public class ConsultingRoomExtendedTests
{
    private const string CorrelationId = "corr-room-ext";
    private const string TrajectoryId = "TRJ-Q1-P1-20260401090000000";

    private static ConsultingRoom CreateActiveRoom(string id = "room-1", string name = "Consultorio A")
    {
        var room = ConsultingRoom.Create(id, name, CorrelationId);
        room.ClearUnraisedEvents();
        return room;
    }

    // ── CallPatient ──────────────────────────────────────────────

    [Fact]
    public void CallPatient_AssignedPatient_RaisesPatientCalledEvent()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);
        room.ClearUnraisedEvents();

        room.CallPatient("PAT-001", "room-1", CorrelationId, TrajectoryId);

        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        Assert.Equal(nameof(PatientCalled), events[0].EventType);
    }

    [Fact]
    public void CallPatient_EmptyTrajectoryId_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);

        Assert.Throws<DomainException>(() =>
            room.CallPatient("PAT-001", "room-1", CorrelationId, ""));
    }

    [Fact]
    public void CallPatient_WrongPatient_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);

        Assert.Throws<DomainException>(() =>
            room.CallPatient("PAT-999", "room-1", CorrelationId, TrajectoryId));
    }

    [Fact]
    public void CallPatient_InactiveRoom_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.Deactivate(CorrelationId);

        Assert.Throws<DomainException>(() =>
            room.CallPatient("PAT-001", "room-1", CorrelationId, TrajectoryId));
    }

    // ── Reactivation ─────────────────────────────────────────────

    [Fact]
    public void Activate_AfterDeactivation_ReactivatesRoom()
    {
        var room = CreateActiveRoom();
        room.Deactivate(CorrelationId);
        room.ClearUnraisedEvents();

        room.Activate(CorrelationId);

        Assert.True(room.IsActive);
        var events = room.GetUnraisedEvents();
        Assert.Single(events);
    }

    [Fact]
    public void Deactivate_WhenPatientBeingAttended_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);

        Assert.Throws<DomainException>(() => room.Deactivate(CorrelationId));
    }

    // ── CompleteAttention ────────────────────────────────────────

    [Fact]
    public void CompleteAttention_ActivePatient_RaisesCompletedEvent()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);
        room.CallPatient("PAT-001", "room-1", CorrelationId, TrajectoryId);
        room.ClearUnraisedEvents();

        room.CompleteAttention("Q1-P1", "Completed", CorrelationId, TrajectoryId);

        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        Assert.Equal(nameof(PatientAttentionCompleted), events[0].EventType);
        Assert.Null(room.CurrentPatientId);
    }

    [Fact]
    public void CompleteAttention_EmptyTrajectoryId_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);
        room.CallPatient("PAT-001", "room-1", CorrelationId, TrajectoryId);

        Assert.Throws<DomainException>(() =>
            room.CompleteAttention("Q1-P1", "Completed", CorrelationId, ""));
    }

    [Fact]
    public void CompleteAttention_NoPatient_ThrowsDomainException()
    {
        var room = CreateActiveRoom();

        Assert.Throws<DomainException>(() =>
            room.CompleteAttention("Q1-P1", "Completed", CorrelationId, TrajectoryId));
    }

    // ── MarkPatientAbsent ────────────────────────────────────────

    [Fact]
    public void MarkPatientAbsent_ActivePatient_RaisesAbsentEvent()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);
        room.CallPatient("PAT-001", "room-1", CorrelationId, TrajectoryId);
        room.ClearUnraisedEvents();

        room.MarkPatientAbsent("Q1-P1", "No se presentó", CorrelationId, TrajectoryId);

        var events = room.GetUnraisedEvents();
        Assert.Single(events);
        Assert.Equal(nameof(PatientAbsentAtConsultation), events[0].EventType);
        Assert.Null(room.CurrentPatientId);
    }

    [Fact]
    public void MarkPatientAbsent_EmptyTrajectoryId_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);
        room.CallPatient("PAT-001", "room-1", CorrelationId, TrajectoryId);

        Assert.Throws<DomainException>(() =>
            room.MarkPatientAbsent("Q1-P1", "No se presentó", CorrelationId, ""));
    }

    // ── Room state after operations ──────────────────────────────

    [Fact]
    public void Create_AutomaticallyActivates()
    {
        var room = ConsultingRoom.Create("room-new", "Consultorio Nuevo", CorrelationId);
        Assert.True(room.IsActive);
    }

    [Fact]
    public void AssignPatient_SetsCurrentPatientAndConsultant()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);

        Assert.Equal("PAT-001", room.CurrentPatientId);
        Assert.Equal("DR-001", room.CurrentConsultantId);
    }

    [Fact]
    public void AssignPatient_WhenOccupied_ThrowsDomainException()
    {
        var room = CreateActiveRoom();
        room.AssignPatient("PAT-001", "DR-001", CorrelationId);

        Assert.Throws<DomainException>(() =>
            room.AssignPatient("PAT-002", "DR-002", CorrelationId));
    }
}
