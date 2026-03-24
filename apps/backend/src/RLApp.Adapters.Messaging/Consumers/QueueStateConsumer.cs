using MassTransit;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Messaging.Consumers;

public class QueueStateConsumer : 
    IConsumer<PatientCheckedIn>,
    IConsumer<PatientAttentionCompleted>,
    IConsumer<PatientAbsentAtConsultation>
{
    private readonly IProjectionStore _projectionStore;

    public QueueStateConsumer(IProjectionStore projectionStore)
    {
        _projectionStore = projectionStore;
    }

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        // Update TotalPending for the queue
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        // Update TotalPending and AverageWaitTime
        await Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<PatientAbsentAtConsultation> context)
    {
        // Update TotalPending
        await Task.CompletedTask;
    }
}
