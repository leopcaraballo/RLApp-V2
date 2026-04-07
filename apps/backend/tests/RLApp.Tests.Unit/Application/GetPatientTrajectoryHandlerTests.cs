namespace RLApp.Tests.Unit.Application;

using NSubstitute;
using RLApp.Application.Handlers;
using RLApp.Application.Queries;
using RLApp.Ports.Outbound;

public class GetPatientTrajectoryHandlerTests
{
    private readonly IProjectionStore _projectionStore = Substitute.For<IProjectionStore>();
    private readonly GetPatientTrajectoryHandler _handler;

    public GetPatientTrajectoryHandlerTests()
    {
        _handler = new GetPatientTrajectoryHandler(_projectionStore);
    }

    [Fact]
    public async Task Handle_WithoutDateFilters_ReturnsAllStages()
    {
        var projection = CreateProjectionWithThreeStages();
        _projectionStore.GetPatientTrajectoryAsync("TRJ-001", Arg.Any<CancellationToken>())
            .Returns(projection);

        var query = new GetPatientTrajectoryQuery("TRJ-001", "corr-1");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.Stages.Count);
    }

    [Fact]
    public async Task Handle_ReturnsCanonicalState()
    {
        var projection = CreateProjectionWithThreeStages();
        _projectionStore.GetPatientTrajectoryAsync("TRJ-001", Arg.Any<CancellationToken>())
            .Returns(projection);

        var query = new GetPatientTrajectoryQuery("TRJ-001", "corr-1");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("TrayectoriaActiva", result.Data!.CurrentState);
    }

    private static PatientTrajectoryProjection CreateProjectionWithThreeStages()
    {
        return new PatientTrajectoryProjection
        {
            TrajectoryId = "TRJ-001",
            PatientId = "PAT-001",
            QueueId = "QUEUE-01",
            CurrentState = "TrayectoriaActiva",
            OpenedAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc),
            CorrelationIds = new[] { "corr-1" },
            Stages = new[]
            {
                new PatientTrajectoryStageProjection
                {
                    OccurredAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc),
                    Stage = "Recepcion",
                    SourceEvent = "PatientCheckedIn",
                    SourceState = "EnEsperaTaquilla",
                    CorrelationId = "corr-1"
                },
                new PatientTrajectoryStageProjection
                {
                    OccurredAt = new DateTime(2026, 4, 1, 9, 15, 0, DateTimeKind.Utc),
                    Stage = "Caja",
                    SourceEvent = "PatientPaymentValidated",
                    SourceState = "EnEsperaConsulta",
                    CorrelationId = "corr-2"
                },
                new PatientTrajectoryStageProjection
                {
                    OccurredAt = new DateTime(2026, 4, 1, 9, 20, 0, DateTimeKind.Utc),
                    Stage = "Consulta",
                    SourceEvent = "PatientClaimedForAttention",
                    SourceState = "EnConsulta",
                    CorrelationId = "corr-3"
                }
            }
        };
    }
}
