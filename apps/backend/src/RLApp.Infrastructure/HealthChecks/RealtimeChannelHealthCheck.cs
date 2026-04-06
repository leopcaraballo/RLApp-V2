using Microsoft.Extensions.Diagnostics.HealthChecks;
using RLApp.Infrastructure.Realtime;

namespace RLApp.Infrastructure.HealthChecks;

public sealed class RealtimeChannelHealthCheck : IHealthCheck
{
    private readonly RealtimeChannelStatus _realtimeChannelStatus;

    public RealtimeChannelHealthCheck(RealtimeChannelStatus realtimeChannelStatus)
    {
        _realtimeChannelStatus = realtimeChannelStatus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = _realtimeChannelStatus.GetSnapshot();

        if (snapshot.ActiveConnections == 0)
        {
            return Task.FromResult(HealthCheckResult.Healthy("No active realtime connections. Channel is registered and awaiting staff clients."));
        }

        var latestPublicationFailed = snapshot.LastPublishFailedAt.HasValue
            && (!snapshot.LastPublishSucceededAt.HasValue || snapshot.LastPublishFailedAt.Value > snapshot.LastPublishSucceededAt.Value);

        if (latestPublicationFailed)
        {
            var degradedDescription = $"Realtime channel has {snapshot.ActiveConnections} active connection(s), but the latest publication of {snapshot.LastEventType ?? "unknown"} to {snapshot.LastDeliveryScope ?? "unknown"} failed with {snapshot.LastFailureType ?? "unknown"}; consecutiveFailures={snapshot.ConsecutivePublishFailures}.";
            return Task.FromResult(HealthCheckResult.Degraded(degradedDescription));
        }

        var healthyDescription = snapshot.LastPublishSucceededAt.HasValue
            ? $"Realtime channel has {snapshot.ActiveConnections} active connection(s); latest publication of {snapshot.LastEventType ?? "unknown"} to {snapshot.LastDeliveryScope ?? "unknown"} succeeded at {snapshot.LastPublishSucceededAt.Value:O}."
            : $"Realtime channel has {snapshot.ActiveConnections} active connection(s); no publications recorded yet.";

        return Task.FromResult(HealthCheckResult.Healthy(healthyDescription));
    }
}
