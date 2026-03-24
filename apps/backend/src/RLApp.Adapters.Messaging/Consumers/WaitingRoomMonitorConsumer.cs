using MassTransit;
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

    public WaitingRoomMonitorConsumer(IProjectionStore projectionStore)
    {
        _projectionStore = projectionStore;
    }

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        var ev = context.Message;
        var data = new Dictionary<string, object>
        {
            { "PatientName", ev.PatientName },
            { "Status", "Waiting" }
        };
        await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", data);
    }

    public async Task Consume(ConsumeContext<PatientCalled> context)
    {
        var ev = context.Message;
        var data = new Dictionary<string, object>
        {
            { "Status", "Called" },
            { "RoomAssigned", ev.RoomId }
        };
        await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", data);
    }

    public async Task Consume(ConsumeContext<PatientClaimedForAttention> context)
    {
        var ev = context.Message;
        var data = new Dictionary<string, object>
        {
            { "Status", "InConsultation" },
            { "RoomAssigned", ev.RoomId }
        };
        await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", data);
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        var ev = context.Message;
        // When attention is completed, we can either update status or delete from monitor
        // For now, update to Completed
        var data = new Dictionary<string, object>
        {
            { "Status", "Completed" }
        };
        await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", data);
        
        // Optionally delete after some time, but for now we keep it or delete it:
        // await _projectionStore.DeleteAsync(ev.PatientId);
    }

    public async Task Consume(ConsumeContext<PatientAbsentAtConsultation> context)
    {
        var ev = context.Message;
        var data = new Dictionary<string, object>
        {
            { "Status", "Absent" }
        };
        await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", data);
    }
}
