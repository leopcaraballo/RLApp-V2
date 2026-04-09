namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;

public class PatientTrajectoryExtendedTests
{
    private const string QueueId = "QUEUE-01";
    private const string PatientId = "PAT-001";
    private const string CorrelationId = "corr-trj-ext";

    private static PatientTrajectory CreateTrajectory(DateTime? at = null)
    {
        var occurredAt = at ?? new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        return PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt),
            PatientId, QueueId,
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn), "EnEsperaTaquilla",
            occurredAt, CorrelationId);
    }

    // ── Allowed transitions ──────────────────────────────────────

    [Fact]
    public void RecordStage_ReceptionToCashier_Succeeds()
    {
        var trj = CreateTrajectory();
        trj.ClearUnraisedEvents();

        var recorded = trj.RecordStage(PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated), "EnEsperaConsulta",
            new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2");

        Assert.True(recorded);
        Assert.Equal(PatientTrajectory.CashierStage, trj.CurrentStage);
    }

    [Fact]
    public void RecordStage_CashierToConsultation_Succeeds()
    {
        var trj = CreateTrajectory();
        trj.RecordStage(PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated), "EnEsperaConsulta",
            new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2");

        var recorded = trj.RecordStage(PatientTrajectory.ConsultationStage,
            nameof(PatientCalled), "LlamadoConsulta",
            new DateTime(2026, 4, 1, 9, 20, 0, DateTimeKind.Utc), "corr-3");

        Assert.True(recorded);
        Assert.Equal(PatientTrajectory.ConsultationStage, trj.CurrentStage);
    }

    // ── Invalid transitions ──────────────────────────────────────

    [Fact]
    public void RecordStage_ReceptionToConsultation_ThrowsDomainException()
    {
        var trj = CreateTrajectory();

        Assert.Throws<DomainException>(() =>
            trj.RecordStage(PatientTrajectory.ConsultationStage,
                nameof(PatientCalled), "LlamadoConsulta",
                new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2"));
    }

    // ── Chronological order ──────────────────────────────────────

    [Fact]
    public void RecordStage_OlderTimestamp_ThrowsDomainException()
    {
        var trj = CreateTrajectory();

        Assert.Throws<DomainException>(() =>
            trj.RecordStage(PatientTrajectory.CashierStage,
                nameof(PatientPaymentValidated), "EnEsperaConsulta",
                new DateTime(2026, 3, 31, 9, 0, 0, DateTimeKind.Utc), "corr-2"));
    }

    // ── Duplicate stage detection ────────────────────────────────

    [Fact]
    public void RecordStage_ExactDuplicate_ReturnsFalse()
    {
        var trj = CreateTrajectory();
        var cashierTime = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);

        // First cashier stage
        trj.RecordStage(PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated), "EnEsperaConsulta",
            cashierTime, "corr-dup");

        // Exact same stage replayed (idempotent duplicate) — should return false
        var duplicate = trj.RecordStage(PatientTrajectory.CashierStage,
            nameof(PatientPaymentValidated), "EnEsperaConsulta",
            cashierTime, "corr-dup");

        Assert.False(duplicate);
    }

    // ── Complete ─────────────────────────────────────────────────

    [Fact]
    public void Complete_ActiveTrajectory_SetsCompletedState()
    {
        var trj = CreateTrajectory();
        trj.RecordStage(PatientTrajectory.CashierStage, nameof(PatientPaymentValidated),
            "EnEsperaConsulta", new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2");
        trj.RecordStage(PatientTrajectory.ConsultationStage, nameof(PatientCalled),
            "LlamadoConsulta", new DateTime(2026, 4, 1, 9, 20, 0, DateTimeKind.Utc), "corr-3");

        var completed = trj.Complete(PatientTrajectory.ConsultationStage,
            nameof(PatientAttentionCompleted), "Finalizado",
            new DateTime(2026, 4, 1, 9, 45, 0, DateTimeKind.Utc), "corr-4");

        Assert.True(completed);
        Assert.Equal(PatientTrajectory.CompletedState, trj.CurrentState);
        Assert.NotNull(trj.ClosedAt);
    }

    [Fact]
    public void Complete_AlreadyCompleted_ReturnsFalse()
    {
        var trj = CreateTrajectory();
        trj.RecordStage(PatientTrajectory.CashierStage, nameof(PatientPaymentValidated),
            "EnEsperaConsulta", new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2");
        trj.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted),
            "Finalizado", new DateTime(2026, 4, 1, 9, 45, 0, DateTimeKind.Utc), "corr-4");

        var secondComplete = trj.Complete(PatientTrajectory.ConsultationStage,
            nameof(PatientAttentionCompleted), "Finalizado",
            new DateTime(2026, 4, 1, 9, 50, 0, DateTimeKind.Utc), "corr-5");

        Assert.False(secondComplete);
    }

    // ── Cancel ───────────────────────────────────────────────────

    [Fact]
    public void Cancel_ActiveTrajectory_SetsCancelledState()
    {
        var trj = CreateTrajectory();

        var cancelled = trj.Cancel(nameof(PatientAbsentAtCashier),
            "CanceladoPorAusencia", "No se presentó",
            new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc), "corr-cancel");

        Assert.True(cancelled);
        Assert.Equal(PatientTrajectory.CancelledState, trj.CurrentState);
        Assert.NotNull(trj.ClosedAt);
    }

    [Fact]
    public void Cancel_CompletedTrajectory_ThrowsDomainException()
    {
        var trj = CreateTrajectory();
        trj.RecordStage(PatientTrajectory.CashierStage, nameof(PatientPaymentValidated),
            "EnEsperaConsulta", new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2");
        trj.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted),
            "Finalizado", new DateTime(2026, 4, 1, 9, 45, 0, DateTimeKind.Utc), "corr-3");

        Assert.Throws<DomainException>(() =>
            trj.Cancel(nameof(PatientAbsentAtCashier), "CanceladoPorAusencia", "reason",
                new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc), "corr-cancel"));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ReturnsFalse()
    {
        var trj = CreateTrajectory();
        trj.Cancel(nameof(PatientAbsentAtCashier), "CanceladoPorAusencia", "No se presentó",
            new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc), "corr-cancel");

        var secondCancel = trj.Cancel(nameof(PatientAbsentAtCashier),
            "CanceladoPorAusencia", "reason2",
            new DateTime(2026, 4, 1, 9, 35, 0, DateTimeKind.Utc), "corr-cancel-2");

        Assert.False(secondCancel);
    }

    // ── Replay ───────────────────────────────────────────────────

    [Fact]
    public void Replay_FromEvents_ReconstructsCorrectState()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var trajectoryId = PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt);

        var events = new List<DomainEvent>
        {
            new PatientTrajectoryOpened(trajectoryId, PatientId, QueueId, occurredAt, "corr-1"),
            new PatientTrajectoryStageRecorded(trajectoryId, PatientId, QueueId,
                PatientTrajectory.ReceptionStage, nameof(PatientCheckedIn), "EnEsperaTaquilla",
                occurredAt, "corr-1")
        };

        var replayed = PatientTrajectory.Replay(events);

        Assert.Equal(trajectoryId, replayed.Id);
        Assert.Equal(PatientTrajectory.ActiveState, replayed.CurrentState);
        Assert.Single(replayed.Stages);
        Assert.Empty(replayed.GetUnraisedEvents());
    }

    [Fact]
    public void Replay_EmptyEvents_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            PatientTrajectory.Replay(Array.Empty<DomainEvent>()));
    }

    // ── RecordRebuild ────────────────────────────────────────────

    [Fact]
    public void RecordRebuild_NewCorrelation_ReturnsTrue()
    {
        var trj = CreateTrajectory();

        var result = trj.RecordRebuild("full",
            new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc), "corr-rebuild");

        Assert.True(result);
    }

    [Fact]
    public void RecordRebuild_DuplicateCorrelation_ReturnsFalse()
    {
        var trj = CreateTrajectory();
        trj.RecordRebuild("full", new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc), "corr-dup-rebuild");

        var result = trj.RecordRebuild("full",
            new DateTime(2026, 4, 1, 10, 1, 0, DateTimeKind.Utc), "corr-dup-rebuild");

        Assert.False(result);
    }

    // ── RecordStage on closed trajectory ─────────────────────────

    [Fact]
    public void RecordStage_OnCompletedTrajectory_ThrowsDomainException()
    {
        var trj = CreateTrajectory();
        trj.RecordStage(PatientTrajectory.CashierStage, nameof(PatientPaymentValidated),
            "EnEsperaConsulta", new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2");
        trj.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted),
            "Finalizado", new DateTime(2026, 4, 1, 9, 45, 0, DateTimeKind.Utc), "corr-3");

        Assert.Throws<DomainException>(() =>
            trj.RecordStage(PatientTrajectory.ConsultationStage, "SomeEvent", "SomeState",
                new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc), "corr-late"));
    }
}
