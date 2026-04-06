using MassTransit;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Messaging.Observability;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Messaging.Consumers;

public class WaitingRoomMonitorConsumer :
    IConsumer<PatientCheckedIn>,
    IConsumer<PatientCalled>,
    IConsumer<PatientClaimedForAttention>,
    IConsumer<PatientAttentionCompleted>,
    IConsumer<PatientAbsentAtConsultation>
{
    private readonly IProjectionStore _projectionStore;
    private readonly ILogger<WaitingRoomMonitorConsumer> _logger;

    public WaitingRoomMonitorConsumer(IProjectionStore projectionStore, ILogger<WaitingRoomMonitorConsumer> logger)
    {
        _projectionStore = projectionStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        var ev = context.Message;
        var turnId = $"{ev.AggregateId}-{ev.PatientId}";
        var data = new Dictionary<string, object>
        {
            { "QueueId", ev.AggregateId },
            { "PatientId", ev.PatientId },
            { "TurnId", turnId },
            { "PatientName", ev.PatientName },
            { "TicketNumber", turnId },
            { "CheckedInAt", ev.OccurredAt },
            { "UpdatedAt", ev.OccurredAt },
            { "Status", "Waiting" }
        };
        await UpsertMonitorAsync(context, turnId, data, "Waiting");
    }

    public async Task Consume(ConsumeContext<PatientCalled> context)
    {
        var ev = context.Message;
        var data = new Dictionary<string, object>
        {
            { "PatientId", ev.PatientId },
            { "UpdatedAt", ev.OccurredAt },
            { "Status", "Called" },
            { "RoomAssigned", ev.RoomId }
        };
        await UpsertMonitorAsync(context, ev.PatientId, data, "Called");
    }

    public async Task Consume(ConsumeContext<PatientClaimedForAttention> context)
    {
        var ev = context.Message;
        var data = new Dictionary<string, object>
        {
            { "PatientId", ev.PatientId },
            { "UpdatedAt", ev.OccurredAt },
            { "Status", "InConsultation" },
            { "RoomAssigned", ev.RoomId }
        };
        await UpsertMonitorAsync(context, ev.PatientId, data, "InConsultation");
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        var ev = context.Message;
        // When attention is completed, we can either update status or delete from monitor
        // For now, update to Completed
        var data = new Dictionary<string, object>
        {
            { "PatientId", ev.PatientId },
            { "TurnId", ev.TurnId ?? $"{ev.AggregateId}-{ev.PatientId}" },
            { "UpdatedAt", ev.OccurredAt },
            { "Status", "Completed" }
        };
        await UpsertMonitorAsync(context, ev.PatientId, data, "Completed");

        // Optionally delete after some time, but for now we keep it or delete it:
        // await _projectionStore.DeleteAsync(ev.PatientId);
    }

    public async Task Consume(ConsumeContext<PatientAbsentAtConsultation> context)
    {
        var ev = context.Message;
        var data = new Dictionary<string, object>
        {
            { "PatientId", ev.PatientId },
            { "TurnId", ev.TurnId ?? $"{ev.AggregateId}-{ev.PatientId}" },
            { "UpdatedAt", ev.OccurredAt },
            { "Status", "Absent" }
        };
        await UpsertMonitorAsync(context, ev.PatientId, data, "Absent");
    }

    private async Task UpsertMonitorAsync<TMessage>(ConsumeContext<TMessage> context, string projectionId, Dictionary<string, object> data, string monitorStatus)
        where TMessage : DomainEvent
    {
        using var activity = MessageFlowTelemetry.StartConsumerActivity(context.Message, nameof(WaitingRoomMonitorConsumer));
        using var scope = MessageFlowTelemetry.BeginScope(
            _logger,
            context.Message,
            "projection-pending",
            consumerName: nameof(WaitingRoomMonitorConsumer));

        try
        {
            await _projectionStore.UpsertAsync(projectionId, "WaitingRoomMonitor", data, context.CancellationToken);
            MessageFlowTelemetry.SetResult(activity, "projection-upserted");
            _logger.LogInformation("Waiting room monitor projection updated to {MonitorStatus}.", monitorStatus);
        }
        catch (Exception ex)
        {
            MessageFlowTelemetry.RecordFailure(activity, ex, "projection-failed");
            _logger.LogError(ex, "Waiting room monitor projection update failed.");
            throw;
        }
    }
}
