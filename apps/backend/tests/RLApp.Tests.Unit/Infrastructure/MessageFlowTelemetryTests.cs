namespace RLApp.Tests.Unit.Infrastructure;

using RLApp.Adapters.Messaging.Observability;
using RLApp.Domain.Events;

public class MessageFlowTelemetryTests
{
    [Fact]
    public void CreateContext_ForConsultationEvent_UsesCanonicalObservabilityFields()
    {
        var message = new PatientAttentionCompleted(
            "QUEUE-01",
            "PAT-01",
            "ROOM-01",
            "TURN-01",
            "completed",
            "CORR-01",
            "TRJ-01");

        var context = MessageFlowTelemetry.CreateContext(
            message,
            "transition-applied",
            sagaName: "ConsultationSaga",
            currentState: "WaitingForPatient",
            nextState: "InConsultation");

        Assert.Equal("CORR-01", context["correlationId"]);
        Assert.Equal("TRJ-01", context["trajectoryId"]);
        Assert.Equal("QUEUE-01", context["queueId"]);
        Assert.Equal("TURN-01", context["turnId"]);
        Assert.Equal(MessageFlowTelemetry.SystemRole, context["role"]);
        Assert.Equal("transition-applied", context["result"]);
        Assert.Equal(nameof(PatientAttentionCompleted), context["messageName"]);
        Assert.Equal("ConsultationSaga", context["sagaName"]);
        Assert.Equal("WaitingForPatient", context["currentState"]);
        Assert.Equal("InConsultation", context["nextState"]);
    }

    [Fact]
    public void CreateContext_ForTrajectoryProjectionEvent_UsesTrajectoryAggregateAsTrajectoryId()
    {
        var message = new PatientTrajectoryStageRecorded(
            "TRJ-01",
            "PAT-01",
            "QUEUE-01",
            "ConsultationCalled",
            nameof(PatientCalled),
            "WaitingForPatient",
            DateTime.UtcNow,
            "CORR-02");

        var context = MessageFlowTelemetry.CreateContext(
            message,
            "projection-upserted",
            consumerName: "PatientTrajectoryConsumer");

        Assert.Equal("TRJ-01", context["trajectoryId"]);
        Assert.Equal("QUEUE-01", context["queueId"]);
        Assert.Null(context["turnId"]);
        Assert.Equal("PatientTrajectoryConsumer", context["consumerName"]);
    }

    [Fact]
    public void CreateContext_DoesNotExposePatientIdentifiersOrNames()
    {
        var message = new PatientCheckedIn(
            "QUEUE-01",
            "PAT-01",
            "Jane Doe",
            "APT-01",
            1,
            null,
            "CORR-03");

        var context = MessageFlowTelemetry.CreateContext(message, "projection-upserted");

        Assert.False(context.ContainsKey("patientId"));
        Assert.False(context.ContainsKey("patientName"));
    }

    [Fact]
    public void NormalizeState_WhenStateIsMissing_ReturnsInitial()
    {
        Assert.Equal("Initial", MessageFlowTelemetry.NormalizeState(null));
        Assert.Equal("Initial", MessageFlowTelemetry.NormalizeState(string.Empty));
    }
}
