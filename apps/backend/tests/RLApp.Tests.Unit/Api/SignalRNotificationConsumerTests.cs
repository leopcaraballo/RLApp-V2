using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RLApp.Api.Consumers;
using RLApp.Api.Hubs;
using RLApp.Domain.Events;
using RLApp.Infrastructure.Realtime;

namespace RLApp.Tests.Unit.Api;

public class SignalRNotificationConsumerTests
{
    [Fact]
    public async Task Consume_WhenPublicationSucceeds_ShouldRecordRealtimeState()
    {
        var hubContext = Substitute.For<IHubContext<NotificationHub>>();
        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        var logger = Substitute.For<ILogger<SignalRNotificationConsumer>>();
        var realtimeChannelStatus = new RealtimeChannelStatus();

        hubContext.Clients.Returns(clients);
        clients.All.Returns(proxy);

        var consumer = new SignalRNotificationConsumer(hubContext, realtimeChannelStatus, logger);
        var context = Substitute.For<ConsumeContext<PatientCheckedIn>>();
        context.Message.Returns(new PatientCheckedIn("queue-1", "patient-1", "Paciente 1", null, 1, null, "corr-1"));
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        var snapshot = realtimeChannelStatus.GetSnapshot();
        Assert.NotNull(snapshot.LastPublishSucceededAt);
        Assert.Equal("PatientCheckedIn", snapshot.LastEventType);
        Assert.Equal("all", snapshot.LastDeliveryScope);
        Assert.Equal(0, snapshot.ConsecutivePublishFailures);
        await proxy.Received(1).SendCoreAsync("PatientCheckedIn", Arg.Any<object?[]>(), CancellationToken.None);
    }

    [Fact]
    public async Task Consume_WhenPublicationFails_ShouldRecordRealtimeFailureState()
    {
        var hubContext = Substitute.For<IHubContext<NotificationHub>>();
        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        var logger = Substitute.For<ILogger<SignalRNotificationConsumer>>();
        var realtimeChannelStatus = new RealtimeChannelStatus();

        hubContext.Clients.Returns(clients);
        clients.All.Returns(proxy);
        proxy.SendCoreAsync("PatientCheckedIn", Arg.Any<object?[]>(), CancellationToken.None)
            .Returns<Task>(_ => throw new InvalidOperationException("socket closed"));

        var consumer = new SignalRNotificationConsumer(hubContext, realtimeChannelStatus, logger);
        var context = Substitute.For<ConsumeContext<PatientCheckedIn>>();
        context.Message.Returns(new PatientCheckedIn("queue-1", "patient-1", "Paciente 1", null, 1, null, "corr-1"));
        context.CancellationToken.Returns(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context));

        var snapshot = realtimeChannelStatus.GetSnapshot();
        Assert.NotNull(snapshot.LastPublishFailedAt);
        Assert.Equal("PatientCheckedIn", snapshot.LastEventType);
        Assert.Equal("all", snapshot.LastDeliveryScope);
        Assert.Equal(1, snapshot.ConsecutivePublishFailures);
        Assert.Equal("InvalidOperationException", snapshot.LastFailureType);
    }
}
