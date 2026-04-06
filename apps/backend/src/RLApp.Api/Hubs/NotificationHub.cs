using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RLApp.Adapters.Http.Security;
using RLApp.Infrastructure.Realtime;

namespace RLApp.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications to the frontend.
/// Used to notify updates on waiting room monitor, dashboard, and queue state.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AuthenticatedStaff)]
public class NotificationHub : Hub
{
    private readonly RealtimeChannelStatus _realtimeChannelStatus;

    public NotificationHub(RealtimeChannelStatus realtimeChannelStatus)
    {
        _realtimeChannelStatus = realtimeChannelStatus;
    }

    public override Task OnConnectedAsync()
    {
        _realtimeChannelStatus.RecordConnectionOpened();
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _realtimeChannelStatus.RecordConnectionClosed();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinQueueGroup(string queueId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{queueId}");
    }

    public async Task LeaveQueueGroup(string queueId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"queue-{queueId}");
    }
}
