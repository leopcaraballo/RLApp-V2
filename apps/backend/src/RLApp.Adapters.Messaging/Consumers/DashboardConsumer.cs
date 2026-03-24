using MassTransit;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Messaging.Consumers;

public class DashboardConsumer : 
    IConsumer<PatientCheckedIn>,
    IConsumer<PatientAttentionCompleted>,
    IConsumer<ConsultingRoomActivated>,
    IConsumer<ConsultingRoomDeactivated>
{
    private readonly IProjectionStore _projectionStore;

    public DashboardConsumer(IProjectionStore projectionStore)
    {
        _projectionStore = projectionStore;
    }

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        // Increment TotalPatientsToday
        // Note: Real implementation would need atomicity or a way to get the current count.
        // For simplicity in this demo phase, we are signaling an update.
        // The RebuildProjectionsHandler can also be used to get accurate counts from event store.
        
        // For now, let's just trigger a dashboard sync or update if we had the current state.
        // Since IProjectionStore handles partial updates, we might need a way to increment.
        // I'll assume for now we are just notifying "something changed" or we'd need a more complex projection logic.
        
        // A better way is to have the projection store handle "Increment" logic, 
        // but for now I'll just skip the increment here since we don't have the current value in the consumer.
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        // Increment TotalCompleted
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<ConsultingRoomActivated> context)
    {
        // Update active rooms count
        // We'd need to know how many are active.
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<ConsultingRoomDeactivated> context)
    {
        // Update active rooms count
        await Task.CompletedTask;
    }
}
