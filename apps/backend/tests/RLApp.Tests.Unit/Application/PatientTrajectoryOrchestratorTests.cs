namespace RLApp.Tests.Unit.Application;

using NSubstitute;
using RLApp.Application.Services;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;

public class PatientTrajectoryOrchestratorTests
{
    private readonly IPatientTrajectoryRepository _trajectoryRepo = Substitute.For<IPatientTrajectoryRepository>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly PatientTrajectoryOrchestrator _sut;

    private const string QueueId = "QUEUE-01";
    private const string PatientId = "PAT-100";
    private const string CorrelationId = "corr-orch-01";

    public PatientTrajectoryOrchestratorTests()
    {
        _sut = new PatientTrajectoryOrchestrator(_trajectoryRepo, _eventPublisher);
    }

    // ── TrackCheckInAsync ──────────────────────────────────────────

    [Fact]
    public async Task TrackCheckIn_NewPatient_CreatesTrajectoryAndPublishes()
    {
        var checkedIn = MakeCheckedIn();
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns((PatientTrajectory?)null);

        await _sut.TrackCheckInAsync(QueueId, checkedIn, CancellationToken.None);

        await _trajectoryRepo.Received(1).AddAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackCheckIn_ExistingActiveTrajectory_ThrowsDomainException()
    {
        var existing = CreateActiveTrajectory();
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.TrackCheckInAsync(QueueId, MakeCheckedIn(), CancellationToken.None));
        Assert.Contains("active trajectory", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── TrackPaymentValidatedAsync ─────────────────────────────────

    [Fact]
    public async Task TrackPaymentValidated_ExistingTrajectory_RecordsStageAndPublishes()
    {
        var trajectory = CreateActiveTrajectory();
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(trajectory);

        var payment = MakePaymentValidated();
        await _sut.TrackPaymentValidatedAsync(QueueId, payment, CancellationToken.None);

        await _trajectoryRepo.Received(1).UpdateAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackPaymentValidated_NoExistingTrajectory_CreatesNewTrajectory()
    {
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns((PatientTrajectory?)null);

        await _sut.TrackPaymentValidatedAsync(QueueId, MakePaymentValidated(), CancellationToken.None);

        await _trajectoryRepo.Received(1).AddAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
    }

    // ── TrackConsultationCalledAsync ───────────────────────────────

    [Fact]
    public async Task TrackConsultationCalled_ExistingTrajectory_RecordsConsultationStage()
    {
        var trajectory = CreateTrajectoryAtCashierStage();
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(trajectory);

        var called = MakePatientCalled();
        await _sut.TrackConsultationCalledAsync(QueueId, called, CancellationToken.None);

        await _trajectoryRepo.Received(1).UpdateAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    // ── TrackConsultationStartedAsync ──────────────────────────────

    [Fact]
    public async Task TrackConsultationStarted_WhenNotStartedPhase_ReturnsEarlyWithoutPersistence()
    {
        var claimed = MakePatientClaimed(phase: PatientClaimedForAttention.ClaimedPhase);
        await _sut.TrackConsultationStartedAsync(QueueId, claimed, CancellationToken.None);

        await _trajectoryRepo.DidNotReceive().FindActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _trajectoryRepo.DidNotReceive().AddAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackConsultationStarted_WhenStartedPhase_RecordsStage()
    {
        var trajectory = CreateTrajectoryAtCashierStage();
        trajectory.RecordStage(PatientTrajectory.ConsultationStage, nameof(PatientCalled), "LlamadoConsulta",
            new DateTime(2026, 4, 1, 9, 20, 0, DateTimeKind.Utc), "corr-call");
        trajectory.ClearUnraisedEvents();

        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(trajectory);

        var claimed = MakePatientClaimed(phase: PatientClaimedForAttention.StartedPhase);
        await _sut.TrackConsultationStartedAsync(QueueId, claimed, CancellationToken.None);

        await _trajectoryRepo.Received(1).UpdateAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
    }

    // ── TrackCompletionAsync ──────────────────────────────────────

    [Fact]
    public async Task TrackCompletion_ExistingTrajectory_CompletesAndPublishes()
    {
        var trajectory = CreateTrajectoryAtConsultationStage();
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(trajectory);

        var completed = MakeAttentionCompleted();
        await _sut.TrackCompletionAsync(QueueId, completed, CancellationToken.None);

        await _trajectoryRepo.Received(1).UpdateAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    // ── TrackCashierAbsenceAsync ──────────────────────────────────

    [Fact]
    public async Task TrackCashierAbsence_ExistingTrajectory_CancelsAndPublishes()
    {
        var trajectory = CreateActiveTrajectory();
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(trajectory);

        var absence = MakeAbsentAtCashier();
        await _sut.TrackCashierAbsenceAsync(QueueId, absence, CancellationToken.None);

        await _trajectoryRepo.Received(1).UpdateAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    // ── TrackConsultationAbsenceAsync ─────────────────────────────

    [Fact]
    public async Task TrackConsultationAbsence_ExistingTrajectory_CancelsAndPublishes()
    {
        var trajectory = CreateTrajectoryAtConsultationStage();
        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(trajectory);

        var absence = MakeAbsentAtConsultation();
        await _sut.TrackConsultationAbsenceAsync(QueueId, absence, CancellationToken.None);

        await _trajectoryRepo.Received(1).UpdateAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    // ── Idempotency ───────────────────────────────────────────────

    [Fact]
    public async Task TrackPaymentValidated_DuplicateEvent_DoesNotPublish()
    {
        var trajectory = CreateActiveTrajectory();
        var payment = MakePaymentValidated();
        // First call records the stage
        trajectory.RecordStage(PatientTrajectory.CashierStage, payment.EventType, "EnEsperaConsulta",
            payment.OccurredAt, payment.CorrelationId);
        trajectory.ClearUnraisedEvents();

        _trajectoryRepo.FindActiveAsync(PatientId, QueueId, Arg.Any<CancellationToken>())
            .Returns(trajectory);

        // Duplicate call with same correlation
        await _sut.TrackPaymentValidatedAsync(QueueId, payment, CancellationToken.None);

        await _trajectoryRepo.DidNotReceive().UpdateAsync(Arg.Any<PatientTrajectory>(), Arg.Any<CancellationToken>());
        await _eventPublisher.DidNotReceive().PublishBatchAsync(Arg.Any<IEnumerable<DomainEvent>>(), Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static PatientCheckedIn MakeCheckedIn(DateTime? at = null)
    {
        var occurredAt = at ?? new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        return new PatientCheckedIn(QueueId, PatientId, "Test Patient", "APP-001", 0, null, CorrelationId)
        {
            OccurredAt = occurredAt
        };
    }

    private static PatientPaymentValidated MakePaymentValidated(DateTime? at = null)
    {
        var occurredAt = at ?? new DateTime(2026, 4, 1, 9, 15, 0, DateTimeKind.Utc);
        return new PatientPaymentValidated(QueueId, PatientId, 50000m, $"{QueueId}-{PatientId}", "PAY-001", "corr-pay-01")
        {
            OccurredAt = occurredAt
        };
    }

    private static PatientCalled MakePatientCalled(DateTime? at = null)
    {
        var occurredAt = at ?? new DateTime(2026, 4, 1, 9, 20, 0, DateTimeKind.Utc);
        return new PatientCalled(QueueId, PatientId, "ROOM-01", "corr-call-01", "TRJ-QUEUE01-PAT100-20260401")
        {
            OccurredAt = occurredAt
        };
    }

    private static PatientClaimedForAttention MakePatientClaimed(string phase = PatientClaimedForAttention.StartedPhase, DateTime? at = null)
    {
        var occurredAt = at ?? new DateTime(2026, 4, 1, 9, 25, 0, DateTimeKind.Utc);
        return new PatientClaimedForAttention("ROOM-01", PatientId, "ROOM-01", "corr-start-01", "TRJ-QUEUE01-PAT100-20260401", phase)
        {
            OccurredAt = occurredAt
        };
    }

    private static PatientAttentionCompleted MakeAttentionCompleted(DateTime? at = null)
    {
        var occurredAt = at ?? new DateTime(2026, 4, 1, 9, 45, 0, DateTimeKind.Utc);
        return new PatientAttentionCompleted("ROOM-01", PatientId, "ROOM-01", $"{QueueId}-{PatientId}", "Completed", "corr-complete-01", "TRJ-QUEUE01-PAT100-20260401")
        {
            OccurredAt = occurredAt
        };
    }

    private static PatientAbsentAtCashier MakeAbsentAtCashier()
    {
        return new PatientAbsentAtCashier(QueueId, PatientId, $"{QueueId}-{PatientId}", "No se presentó", "corr-absent-cashier")
        {
            OccurredAt = new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc)
        };
    }

    private static PatientAbsentAtConsultation MakeAbsentAtConsultation()
    {
        return new PatientAbsentAtConsultation("ROOM-01", PatientId, $"{QueueId}-{PatientId}", "No se presentó a consulta", "corr-absent-consult", "TRJ-QUEUE01-PAT100-20260401")
        {
            OccurredAt = new DateTime(2026, 4, 1, 9, 35, 0, DateTimeKind.Utc)
        };
    }

    private static PatientTrajectory CreateActiveTrajectory()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt),
            PatientId, QueueId,
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn), "EnEsperaTaquilla",
            occurredAt, CorrelationId);
        trajectory.ClearUnraisedEvents();
        return trajectory;
    }

    private static PatientTrajectory CreateTrajectoryAtCashierStage()
    {
        var trajectory = CreateActiveTrajectory();
        trajectory.RecordStage(PatientTrajectory.CashierStage, nameof(PatientPaymentValidated),
            "EnEsperaConsulta", new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-pay");
        trajectory.ClearUnraisedEvents();
        return trajectory;
    }

    private static PatientTrajectory CreateTrajectoryAtConsultationStage()
    {
        var trajectory = CreateTrajectoryAtCashierStage();
        trajectory.RecordStage(PatientTrajectory.ConsultationStage, nameof(PatientCalled),
            "LlamadoConsulta", new DateTime(2026, 4, 1, 9, 20, 0, DateTimeKind.Utc), "corr-call");
        trajectory.ClearUnraisedEvents();
        return trajectory;
    }
}
