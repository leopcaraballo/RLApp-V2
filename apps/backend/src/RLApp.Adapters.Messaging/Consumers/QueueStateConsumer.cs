using MassTransit;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Messaging.Observability;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Messaging.Consumers;

public class QueueStateConsumer :
    IConsumer<PatientCheckedIn>,
    IConsumer<PatientAttentionCompleted>,
    IConsumer<PatientAbsentAtConsultation>
{
    private readonly IProjectionStore _projectionStore;
    private readonly ILogger<QueueStateConsumer> _logger;

    public QueueStateConsumer(IProjectionStore projectionStore, ILogger<QueueStateConsumer> logger)
    {
        _projectionStore = projectionStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        await RecordNoOpAsync(context);
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        await RecordNoOpAsync(context);
    }

    public async Task Consume(ConsumeContext<PatientAbsentAtConsultation> context)
    {
        await RecordNoOpAsync(context);
    }

    private Task RecordNoOpAsync<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : DomainEvent
    {
        using var activity = MessageFlowTelemetry.StartConsumerActivity(
            context.Message,
            nameof(QueueStateConsumer),
            "projection-noop");
        using var scope = MessageFlowTelemetry.BeginScope(
            _logger,
            context.Message,
            "projection-noop",
            consumerName: nameof(QueueStateConsumer));

        _logger.LogInformation("Queue state consumer observed a message without a projection mutation.");
        return Task.CompletedTask;
    }
}
