namespace RLApp.Tests.Unit.Application;

using NSubstitute;
using RLApp.Application.Services;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;

public class PatientTrajectoryCorrelationResolverTests
{
    private readonly IPatientTrajectoryRepository _trajectoryRepository = Substitute.For<IPatientTrajectoryRepository>();

    [Fact]
    public async Task ResolveRequiredAsync_WhenTrajectoryExists_ReturnsTrajectoryId()
    {
        var occurredAt = new DateTime(2026, 4, 5, 9, 0, 0, DateTimeKind.Utc);
        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create("QUEUE-01", "PAT-01", occurredAt),
            "PAT-01",
            "QUEUE-01",
            PatientTrajectory.ReceptionStage,
            nameof(PatientCheckedIn),
            "EnEsperaTaquilla",
            occurredAt,
            "corr-1");

        _trajectoryRepository.FindActiveAsync("PAT-01", "QUEUE-01", Arg.Any<CancellationToken>())
            .Returns(trajectory);

        var resolver = new PatientTrajectoryCorrelationResolver(_trajectoryRepository);

        var result = await resolver.ResolveRequiredAsync("PAT-01", "QUEUE-01", CancellationToken.None);

        Assert.Equal(trajectory.Id, result);
    }

    [Fact]
    public async Task ResolveRequiredAsync_WhenTrajectoryIsMissing_ThrowsDomainException()
    {
        _trajectoryRepository.FindActiveAsync("PAT-01", "QUEUE-01", Arg.Any<CancellationToken>())
            .Returns((PatientTrajectory?)null);

        var resolver = new PatientTrajectoryCorrelationResolver(_trajectoryRepository);

        await Assert.ThrowsAsync<DomainException>(() =>
            resolver.ResolveRequiredAsync("PAT-01", "QUEUE-01", CancellationToken.None));
    }
}