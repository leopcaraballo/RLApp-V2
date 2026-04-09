namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;

public class PatientTrajectoryTests
{
    private const string QueueId = "QUEUE-01";
    private const string PatientId = "PAT-001";
    private const string CorrelationId = "corr-001";

    [Fact]
    public void Start_ValidInput_RaisesOpenedAndInitialStageEvents()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);

        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt),
            PatientId,
            QueueId,
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn),
            "EnEsperaTaquilla",
            occurredAt,
            CorrelationId);

        Assert.Equal(PatientTrajectory.ActiveState, trajectory.CurrentState);
        Assert.Single(trajectory.Stages);

        var events = trajectory.GetUnraisedEvents();
        Assert.Equal(2, events.Count);
        Assert.IsType<PatientTrajectoryOpened>(events[0]);
        Assert.IsType<PatientTrajectoryStageRecorded>(events[1]);
    }

    [Fact]
    public void RecordStage_DuplicateStage_ReturnsFalseWithoutAddingANewStage()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();

        var added = trajectory.RecordStage(
            PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated),
            "EnEsperaConsulta",
            occurredAt.AddMinutes(5),
            "corr-002");
        Assert.True(added);

        trajectory.ClearUnraisedEvents();

        var duplicate = trajectory.RecordStage(
            PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated),
            "EnEsperaConsulta",
            occurredAt.AddMinutes(5),
            "corr-002");

        Assert.False(duplicate);
        Assert.Equal(2, trajectory.Stages.Count);
        Assert.Empty(trajectory.GetUnraisedEvents());
    }

    [Fact]
    public void RecordStage_ClosedTrajectory_ThrowsDomainException()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();
        trajectory.Complete(
            PatientTrajectory.ConsultationStage,
            nameof(PatientAttentionCompleted),
            "Finalizado",
            occurredAt.AddMinutes(20),
            "corr-003");

        Assert.Throws<DomainException>(() => trajectory.RecordStage(
            PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated),
            "EnEsperaConsulta",
            occurredAt.AddMinutes(21),
            "corr-004"));
    }

    [Fact]
    public void RecordRebuild_NewCorrelation_RaisesRebuiltEvent()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();

        var rebuilt = trajectory.RecordRebuild("queue:QUEUE-01", occurredAt.AddMinutes(30), "corr-005");

        Assert.True(rebuilt);
        var events = trajectory.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<PatientTrajectoryRebuilt>(events[0]);
    }

    [Fact]
    public void RecordStage_ValidTransition_ReceptionToCashier_Succeeds()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();

        var added = trajectory.RecordStage(
            PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated),
            "EnEsperaConsulta",
            occurredAt.AddMinutes(5),
            "corr-010");

        Assert.True(added);
        Assert.Equal(PatientTrajectory.CashierStage, trajectory.CurrentStage);
    }

    [Fact]
    public void RecordStage_InvalidTransition_ReceptionToConsultation_ThrowsDomainException()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();

        var ex = Assert.Throws<DomainException>(() => trajectory.RecordStage(
            PatientTrajectory.ConsultationStage,
            nameof(PatientClaimedForAttention),
            "EnEsperaConsulta",
            occurredAt.AddMinutes(5),
            "corr-011"));

        Assert.Contains("Invalid stage transition", ex.Message);
    }

    [Fact]
    public void RecordStage_ValidTransition_CashierToConsultation_Succeeds()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();

        trajectory.RecordStage(
            PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated),
            "EnEsperaConsulta",
            occurredAt.AddMinutes(5),
            "corr-012");

        var added = trajectory.RecordStage(
            PatientTrajectory.ConsultationStage,
            nameof(PatientCalled),
            "LlamadoConsulta",
            occurredAt.AddMinutes(10),
            "corr-013");

        Assert.True(added);
        Assert.Equal(PatientTrajectory.ConsultationStage, trajectory.CurrentStage);
    }

    [Fact]
    public void RecordStage_ConsultationCanAdvanceFromCalledToStarted_Succeeds()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();

        trajectory.RecordStage(
            PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated),
            "EnEsperaConsulta",
            occurredAt.AddMinutes(5),
            "corr-012");

        trajectory.RecordStage(
            PatientTrajectory.ConsultationStage,
            nameof(PatientCalled),
            "LlamadoConsulta",
            occurredAt.AddMinutes(10),
            "corr-013");

        var started = trajectory.RecordStage(
            PatientTrajectory.ConsultationStage,
            nameof(PatientClaimedForAttention),
            "EnConsulta",
            occurredAt.AddMinutes(11),
            "corr-014");

        Assert.True(started);
        Assert.Equal(4, trajectory.Stages.Count);
        Assert.Equal("EnConsulta", trajectory.Stages[^1].SourceState);
    }

    [Fact]
    public void CurrentStage_AfterStart_ReturnsInitialStage()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        Assert.Equal(PatientTrajectory.ReceptionStage, trajectory.CurrentStage);
    }

    // --- Replay tests ---

    [Fact]
    public void Replay_FromOpenedAndStageEvents_ReconstructsTrajectory()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectoryId = PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt);

        var events = new List<DomainEvent>
        {
            new PatientTrajectoryOpened(trajectoryId, PatientId, QueueId, occurredAt, "corr-r1"),
            new PatientTrajectoryStageRecorded(trajectoryId, PatientId, QueueId, PatientTrajectory.ReceptionStage, nameof(PatientCheckedIn), "EnEsperaTaquilla", occurredAt, "corr-r1"),
            new PatientTrajectoryStageRecorded(trajectoryId, PatientId, QueueId, PatientTrajectory.CashierStage, nameof(PatientPaymentValidated), "EnEsperaConsulta", occurredAt.AddMinutes(5), "corr-r2")
        };

        var replayed = PatientTrajectory.Replay(events);

        Assert.Equal(trajectoryId, replayed.Id);
        Assert.Equal(PatientId, replayed.PatientId);
        Assert.Equal(QueueId, replayed.QueueId);
        Assert.Equal(PatientTrajectory.ActiveState, replayed.CurrentState);
        Assert.Equal(2, replayed.Stages.Count);
        Assert.Equal(PatientTrajectory.CashierStage, replayed.CurrentStage);
        Assert.Equal(3, replayed.Version);
        Assert.Empty(replayed.GetUnraisedEvents());
    }

    [Fact]
    public void Replay_CompletedTrajectory_SetsCompletedState()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectoryId = PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt);

        var events = new List<DomainEvent>
        {
            new PatientTrajectoryOpened(trajectoryId, PatientId, QueueId, occurredAt, "corr-c1"),
            new PatientTrajectoryStageRecorded(trajectoryId, PatientId, QueueId, PatientTrajectory.ReceptionStage, nameof(PatientCheckedIn), "EnEsperaTaquilla", occurredAt, "corr-c1"),
            new PatientTrajectoryCompleted(trajectoryId, PatientId, QueueId, PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted), "Finalizado", occurredAt.AddMinutes(30), "corr-c3")
        };

        var replayed = PatientTrajectory.Replay(events);

        Assert.Equal(PatientTrajectory.CompletedState, replayed.CurrentState);
        Assert.NotNull(replayed.ClosedAt);
    }

    [Fact]
    public void Replay_CancelledTrajectory_SetsCancelledState()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectoryId = PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt);

        var events = new List<DomainEvent>
        {
            new PatientTrajectoryOpened(trajectoryId, PatientId, QueueId, occurredAt, "corr-x1"),
            new PatientTrajectoryStageRecorded(trajectoryId, PatientId, QueueId, PatientTrajectory.ReceptionStage, nameof(PatientCheckedIn), "EnEsperaTaquilla", occurredAt, "corr-x1"),
            new PatientTrajectoryCancelled(trajectoryId, PatientId, QueueId, nameof(PatientAbsentAtCashier), null, "Patient absent", occurredAt.AddMinutes(15), "corr-x2")
        };

        var replayed = PatientTrajectory.Replay(events);

        Assert.Equal(PatientTrajectory.CancelledState, replayed.CurrentState);
        Assert.NotNull(replayed.ClosedAt);
    }

    [Fact]
    public void Replay_WithoutOpenedEvent_ThrowsDomainException()
    {
        var events = new List<DomainEvent>
        {
            new PatientTrajectoryStageRecorded("t1", "p1", "q1", "Recepcion", "CheckIn", null, DateTime.UtcNow, "c1")
        };

        Assert.Throws<DomainException>(() => PatientTrajectory.Replay(events));
    }

    [Fact]
    public void Replay_DuplicateStageEvents_DeduplicatesStages()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectoryId = PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt);

        var stageEvent = new PatientTrajectoryStageRecorded(
            trajectoryId, PatientId, QueueId, PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn), "EnEsperaTaquilla", occurredAt, "corr-d1");

        var events = new List<DomainEvent>
        {
            new PatientTrajectoryOpened(trajectoryId, PatientId, QueueId, occurredAt, "corr-d1"),
            stageEvent,
            stageEvent // duplicate
        };

        var replayed = PatientTrajectory.Replay(events);

        Assert.Single(replayed.Stages);
    }

    // --- Cancel tests ---

    [Fact]
    public void Cancel_ActiveTrajectory_SetsCancelledState()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.ClearUnraisedEvents();

        var cancelled = trajectory.Cancel(
            nameof(PatientAbsentAtCashier), null, "Patient absent",
            occurredAt.AddMinutes(10), "corr-cancel-1");

        Assert.True(cancelled);
        Assert.Equal(PatientTrajectory.CancelledState, trajectory.CurrentState);
        Assert.NotNull(trajectory.ClosedAt);
        var events = trajectory.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<PatientTrajectoryCancelled>(events[0]);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ReturnsFalse()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.Cancel(nameof(PatientAbsentAtCashier), null, "absent", occurredAt.AddMinutes(10), "corr-c1");
        trajectory.ClearUnraisedEvents();

        var result = trajectory.Cancel(nameof(PatientAbsentAtCashier), null, "absent", occurredAt.AddMinutes(11), "corr-c2");

        Assert.False(result);
        Assert.Empty(trajectory.GetUnraisedEvents());
    }

    [Fact]
    public void Cancel_CompletedTrajectory_ThrowsDomainException()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted), "Finalizado", occurredAt.AddMinutes(20), "corr-cmp");

        Assert.Throws<DomainException>(() =>
            trajectory.Cancel(nameof(PatientAbsentAtCashier), null, "absent", occurredAt.AddMinutes(21), "corr-cnl"));
    }

    // --- Complete idempotency tests ---

    [Fact]
    public void Complete_AlreadyCompleted_ReturnsFalse()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted), "Finalizado", occurredAt.AddMinutes(20), "corr-cmp1");
        trajectory.ClearUnraisedEvents();

        var result = trajectory.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted), "Finalizado", occurredAt.AddMinutes(21), "corr-cmp2");

        Assert.False(result);
        Assert.Empty(trajectory.GetUnraisedEvents());
    }

    [Fact]
    public void Complete_CancelledTrajectory_ThrowsDomainException()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var trajectory = CreateTrajectory(occurredAt);
        trajectory.Cancel(nameof(PatientAbsentAtCashier), null, "absent", occurredAt.AddMinutes(10), "corr-cnl");

        Assert.Throws<DomainException>(() =>
            trajectory.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted), "Finalizado", occurredAt.AddMinutes(20), "corr-cmp"));
    }

    private static PatientTrajectory CreateTrajectory(DateTime occurredAt)
    {
        return PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt),
            PatientId,
            QueueId,
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn),
            "EnEsperaTaquilla",
            occurredAt,
            CorrelationId);
    }
}
