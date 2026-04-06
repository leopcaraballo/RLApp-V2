using System.Diagnostics;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RLApp.Api.Hubs;
using RLApp.Adapters.Messaging.Observability;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Infrastructure.Realtime;

namespace RLApp.Api.Consumers;

/// <summary>
/// Consumes domain events and pushes notifications to SignalR clients.
/// This decouples the persistence projections from the real-time UI updates.
/// </summary>
public class SignalRNotificationConsumer :
    IConsumer<PatientCheckedIn>,
    IConsumer<PatientCalled>,
    IConsumer<PatientClaimedForAttention>,
    IConsumer<PatientAttentionCompleted>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly RealtimeChannelStatus _realtimeChannelStatus;
    private readonly ILogger<SignalRNotificationConsumer> _logger;

    public SignalRNotificationConsumer(
        IHubContext<NotificationHub> hubContext,
        RealtimeChannelStatus realtimeChannelStatus,
        ILogger<SignalRNotificationConsumer> logger)
    {
        _hubContext = hubContext;
        _realtimeChannelStatus = realtimeChannelStatus;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        var ev = context.Message;
        var payload = new
        {
            ev.EventType,
            ev.AggregateId,
            ev.CorrelationId,
            ev.TrajectoryId,
            ev.OccurredAt,
            ev.PatientId,
            ev.PatientName,
            QueueId = ev.AggregateId,
            Status = "Waiting"
        };

        await PublishScopedAsync(context, "PatientCheckedIn", payload, ev.AggregateId, ev.TrajectoryId);
    }

    public async Task Consume(ConsumeContext<PatientCalled> context)
    {
        var ev = context.Message;
        var payload = new
        {
            ev.EventType,
            ev.AggregateId,
            ev.CorrelationId,
            ev.TrajectoryId,
            ev.OccurredAt,
            ev.PatientId,
            ev.RoomId,
            QueueId = ev.AggregateId,
            Status = "Called"
        };

        await PublishScopedAsync(context, "PatientCalled", payload, ev.AggregateId, ev.TrajectoryId);
    }

    public async Task Consume(ConsumeContext<PatientClaimedForAttention> context)
    {
        var ev = context.Message;
        var payload = new
        {
            ev.EventType,
            ev.AggregateId,
            ev.CorrelationId,
            ev.TrajectoryId,
            ev.OccurredAt,
            ev.PatientId,
            ev.RoomId,
            QueueId = ev.AggregateId,
            Status = "InConsultation"
        };

        await PublishScopedAsync(context, "PatientAtConsultation", payload, ev.AggregateId, ev.TrajectoryId);
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        var ev = context.Message;
        var payload = new
        {
            ev.EventType,
            ev.AggregateId,
            ev.CorrelationId,
            ev.TrajectoryId,
            ev.OccurredAt,
            ev.PatientId,
            QueueId = ev.AggregateId,
            Status = "Completed"
        };

        await PublishScopedAsync(context, "PatientAttentionCompleted", payload, ev.AggregateId, ev.TrajectoryId);
    }

    private async Task PublishScopedAsync<TEvent>(
        ConsumeContext<TEvent> context,
        string methodName,
        object payload,
        string queueId,
        string? trajectoryId)
        where TEvent : DomainEvent
    {
        await PublishAsync(
            context,
            clients => clients.Group(NotificationHub.DashboardGroup),
            NotificationHub.DashboardGroup,
            methodName,
            payload);

        var queueGroup = NotificationHub.QueueGroup(queueId);
        await PublishAsync(
            context,
            clients => clients.Group(queueGroup),
            queueGroup,
            methodName,
            payload);

        if (!string.IsNullOrWhiteSpace(trajectoryId))
        {
            var trajectoryGroup = NotificationHub.TrajectoryGroup(trajectoryId);
            await PublishAsync(
                context,
                clients => clients.Group(trajectoryGroup),
                trajectoryGroup,
                methodName,
                payload);
        }
    }

    private async Task PublishAsync<TEvent>(
        ConsumeContext<TEvent> context,
        Func<IHubClients, IClientProxy> targetFactory,
        string deliveryScope,
        string methodName,
        object payload)
        where TEvent : DomainEvent
    {
        using var activity = MessageFlowTelemetry.StartConsumerActivity(context.Message, nameof(SignalRNotificationConsumer), "realtime-publish-pending");
        activity?.SetTag("delivery.scope", deliveryScope);
        activity?.SetTag("realtime.method", methodName);

        using var scope = MessageFlowTelemetry.BeginScope(
            _logger,
            context.Message,
            "realtime-publish-pending",
            consumerName: nameof(SignalRNotificationConsumer));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await targetFactory(_hubContext.Clients).SendAsync(methodName, payload, context.CancellationToken);

            _realtimeChannelStatus.RecordPublishSucceeded(context.Message.EventType, deliveryScope, stopwatch.Elapsed);
            MessageFlowTelemetry.SetResult(activity, "realtime-published");

            _logger.LogInformation(
                "Realtime notification published for {EventType} to {DeliveryScope}.",
                context.Message.EventType,
                deliveryScope);
        }
        catch (Exception ex)
        {
            _realtimeChannelStatus.RecordPublishFailed(context.Message.EventType, deliveryScope, stopwatch.Elapsed, ex);
            MessageFlowTelemetry.RecordFailure(activity, ex, "realtime-publish-failed");

            _logger.LogError(
                ex,
                "Realtime notification failed for {EventType} to {DeliveryScope}.",
                context.Message.EventType,
                deliveryScope);

            throw;
        }
    }
}
