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
