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
        // Not yet implemented — projection increment requires snapshot or current-value query.
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        // Not yet implemented.
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<ConsultingRoomActivated> context)
    {
        // Not yet implemented.
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<ConsultingRoomDeactivated> context)
    {
        // Not yet implemented.
        await Task.CompletedTask;
    }
}
