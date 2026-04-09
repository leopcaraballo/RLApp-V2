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
    internal const string DashboardGroup = "dashboard";
    private readonly RealtimeChannelStatus _realtimeChannelStatus;
    private static readonly string[] DashboardRoles = ["Support", "Supervisor"];
    private static readonly string[] QueueRoles = ["Receptionist", "Doctor", "Supervisor"];
    private static readonly string[] TrajectoryRoles = ["Support", "Supervisor"];

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

    public async Task JoinDashboardGroup()
    {
        EnsureRole(DashboardRoles, "dashboard invalidations");
        await Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup);
    }

    public async Task LeaveDashboardGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DashboardGroup);
    }

    public async Task JoinQueueGroup(string queueId)
    {
        EnsureRole(QueueRoles, "queue invalidations");

        if (string.IsNullOrWhiteSpace(queueId))
        {
            throw new HubException("Queue ID is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, QueueGroup(queueId));
    }

    public async Task LeaveQueueGroup(string queueId)
    {
        if (string.IsNullOrWhiteSpace(queueId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, QueueGroup(queueId));
    }

    public async Task JoinTrajectoryGroup(string trajectoryId)
    {
        EnsureRole(TrajectoryRoles, "trajectory invalidations");

        if (string.IsNullOrWhiteSpace(trajectoryId))
        {
            throw new HubException("Trajectory ID is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TrajectoryGroup(trajectoryId));
    }

    public async Task LeaveTrajectoryGroup(string trajectoryId)
    {
        if (string.IsNullOrWhiteSpace(trajectoryId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TrajectoryGroup(trajectoryId));
    }

    internal static string QueueGroup(string queueId) => $"queue-{queueId}";

    internal static string TrajectoryGroup(string trajectoryId) => $"trajectory-{trajectoryId}";

    private void EnsureRole(IEnumerable<string> allowedRoles, string scope)
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
        {
            throw new HubException("Authentication is required.");
        }

        if (!allowedRoles.Any(role => Context.User.IsInRole(role)))
        {
            throw new HubException($"Current role cannot subscribe to {scope}.");
        }
    }
}
