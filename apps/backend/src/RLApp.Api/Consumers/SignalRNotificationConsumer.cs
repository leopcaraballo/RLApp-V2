using MassTransit;
using Microsoft.AspNetCore.SignalR;
using RLApp.Api.Hubs;
using RLApp.Domain.Events;

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

    public SignalRNotificationConsumer(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        var ev = context.Message;
        await _hubContext.Clients.All.SendAsync("PatientCheckedIn", new {
            ev.PatientId,
            ev.PatientName,
            Status = "Waiting"
        });
    }

    public async Task Consume(ConsumeContext<PatientCalled> context)
    {
        var ev = context.Message;
        await _hubContext.Clients.All.SendAsync("PatientCalled", new {
            ev.PatientId,
            ev.RoomId,
            Status = "Called"
        });
    }

    public async Task Consume(ConsumeContext<PatientClaimedForAttention> context)
    {
        var ev = context.Message;
        await _hubContext.Clients.Group($"queue-{ev.AggregateId}").SendAsync("PatientAtConsultation", new {
            ev.PatientId,
            ev.RoomId,
            Status = "InConsultation"
        });
    }

    public async Task Consume(ConsumeContext<PatientAttentionCompleted> context)
    {
        var ev = context.Message;
        await _hubContext.Clients.All.SendAsync("PatientAttentionCompleted", new {
            ev.PatientId,
            Status = "Completed"
        });
    }
}
