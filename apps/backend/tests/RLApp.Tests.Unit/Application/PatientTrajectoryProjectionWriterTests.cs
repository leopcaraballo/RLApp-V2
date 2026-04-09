namespace RLApp.Tests.Unit.Application;

using NSubstitute;
using RLApp.Application.Services;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

public class PatientTrajectoryProjectionWriterTests
{
    private const string QueueId = "QUEUE-01";
    private const string PatientId = "PAT-001";
    private const string CorrelationId = "corr-proj";

    private readonly IPatientTrajectoryRepository _trajectoryRepo = Substitute.For<IPatientTrajectoryRepository>();
    private readonly IProjectionStore _projectionStore = Substitute.For<IProjectionStore>();
    private readonly PatientTrajectoryProjectionWriter _sut;

    public PatientTrajectoryProjectionWriterTests()
    {
        _sut = new PatientTrajectoryProjectionWriter(_trajectoryRepo, _projectionStore);
    }

    // ── Map ──────────────────────────────────────────────────────

    [Fact]
    public void Map_ActiveTrajectory_MapsAllProperties()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt),
            PatientId, QueueId,
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn), "EnEsperaTaquilla",
            occurredAt, CorrelationId);

        var projection = PatientTrajectoryProjectionWriter.Map(trajectory);

        Assert.Equal(trajectory.Id, projection.TrajectoryId);
        Assert.Equal(PatientId, projection.PatientId);
        Assert.Equal(QueueId, projection.QueueId);
        Assert.Equal(PatientTrajectory.ActiveState, projection.CurrentState);
        Assert.Equal(occurredAt, projection.OpenedAt);
        Assert.Null(projection.ClosedAt);
        Assert.Single(projection.Stages);
        Assert.Contains(CorrelationId, projection.CorrelationIds);
    }

    [Fact]
    public void Map_CompletedTrajectory_IncludesClosedAt()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt),
            PatientId, QueueId,
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn), "EnEsperaTaquilla",
            occurredAt, CorrelationId);

        trajectory.RecordStage(PatientTrajectory.CashierStage, nameof(PatientPaymentValidated),
            "EnEsperaConsulta", new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc), "corr-2");
        trajectory.Complete(PatientTrajectory.ConsultationStage, nameof(PatientAttentionCompleted),
            "Finalizado", new DateTime(2026, 4, 1, 9, 45, 0, DateTimeKind.Utc), "corr-3");

        var projection = PatientTrajectoryProjectionWriter.Map(trajectory);

        Assert.Equal(PatientTrajectory.CompletedState, projection.CurrentState);
        Assert.NotNull(projection.ClosedAt);
    }

    [Fact]
    public void Map_MultipleStages_OrdersChronologically()
    {
        var t0 = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, t0),
            PatientId, QueueId,
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn), "EnEsperaTaquilla", t0, "corr-1");

        trajectory.RecordStage(PatientTrajectory.CashierStage, nameof(PatientPaymentValidated),
            "EnEsperaConsulta", new DateTime(2026, 4, 1, 9, 15, 0, DateTimeKind.Utc), "corr-2");
        trajectory.RecordStage(PatientTrajectory.ConsultationStage, nameof(PatientCalled),
            "LlamadoConsulta", new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc), "corr-3");

        var projection = PatientTrajectoryProjectionWriter.Map(trajectory);

        Assert.Equal(3, projection.Stages.Count);
        Assert.True(projection.Stages[0].OccurredAt <= projection.Stages[1].OccurredAt);
        Assert.True(projection.Stages[1].OccurredAt <= projection.Stages[2].OccurredAt);
    }

    // ── RefreshAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_LoadsTrajectoryAndUpsertsProjection()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var trajectoryId = PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt);
        var trajectory = PatientTrajectory.Start(trajectoryId, PatientId, QueueId,
            PatientTrajectory.ReceptionStage, nameof(PatientCheckedIn), "EnEsperaTaquilla",
            occurredAt, CorrelationId);

        _trajectoryRepo.GetByIdAsync(trajectoryId, Arg.Any<CancellationToken>()).Returns(trajectory);

        await _sut.RefreshAsync(trajectoryId, CancellationToken.None);

        await _projectionStore.Received(1).UpsertAsync(
            trajectoryId, "PatientTrajectory", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── UpsertAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_WritesProjection()
    {
        var occurredAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(QueueId, PatientId, occurredAt),
            PatientId, QueueId,
            PatientTrajectory.ReceptionStage, nameof(PatientCheckedIn), "EnEsperaTaquilla",
            occurredAt, CorrelationId);

        await _sut.UpsertAsync(trajectory, CancellationToken.None);

        await _projectionStore.Received(1).UpsertAsync(
            trajectory.Id, "PatientTrajectory", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
