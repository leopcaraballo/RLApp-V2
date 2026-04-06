namespace RLApp.Tests.Unit.Infrastructure;

using RLApp.Adapters.Messaging.Sagas;

public class ConsultationSagaTests
{
    [Fact]
    public void BuildSagaCorrelationId_WithSameTrajectoryId_ReturnsSameGuid()
    {
        var first = ConsultationSaga.BuildSagaCorrelationId("TRJ-Q-1-P-1-20260405090000000", "PAT-01");
        var second = ConsultationSaga.BuildSagaCorrelationId("TRJ-Q-1-P-1-20260405090000000", "PAT-99");

        Assert.Equal(first, second);
    }

    [Fact]
    public void BuildSagaCorrelationId_WithoutTrajectoryId_FallsBackToPatientId()
    {
        var first = ConsultationSaga.BuildSagaCorrelationId(null, "PAT-01");
        var second = ConsultationSaga.BuildSagaCorrelationId(string.Empty, "PAT-01");
        var third = ConsultationSaga.BuildSagaCorrelationId(null, "PAT-02");

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }
}