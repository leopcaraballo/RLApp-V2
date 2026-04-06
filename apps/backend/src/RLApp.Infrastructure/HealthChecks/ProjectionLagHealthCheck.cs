using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RLApp.Adapters.Persistence.Data;

namespace RLApp.Infrastructure.HealthChecks;

/// <summary>
/// Health check that validates projection lag by comparing latest event
/// timestamp with the latest projection update timestamp.
/// </summary>
public class ProjectionLagHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public ProjectionLagHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latestEventAt = await _context.EventStore
                .AsNoTracking()
                .OrderByDescending(e => e.OccurredAt)
                .Select(e => (DateTime?)e.OccurredAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestEventAt == null)
            {
                return HealthCheckResult.Healthy("No events found in store.");
            }

            var latestQueueUpdate = await _context.QueueStates
                .AsNoTracking()
                .Select(q => (DateTime?)q.LastUpdatedAt)
                .MaxAsync(cancellationToken);

            var latestMonitorUpdate = await _context.WaitingRoomMonitors
                .AsNoTracking()
                .Select(w => (DateTime?)w.UpdatedAt)
                .MaxAsync(cancellationToken);

            var latestUpdate = new[] { latestQueueUpdate, latestMonitorUpdate }
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            if (latestUpdate == DateTime.MinValue)
            {
                var initialMaterializationLag = DateTime.UtcNow - latestEventAt.Value;

                if (initialMaterializationLag > TimeSpan.FromSeconds(120))
                {
                    return HealthCheckResult.Unhealthy(
                        $"Projections have not been materialized and latest event age is {initialMaterializationLag.TotalSeconds:F0} seconds.");
                }

                return HealthCheckResult.Degraded(
                    $"Projections have not been updated yet. Latest event age is {initialMaterializationLag.TotalSeconds:F0} seconds.");
            }

            var lag = latestEventAt.Value - latestUpdate;
            if (lag < TimeSpan.Zero)
            {
                lag = TimeSpan.Zero;
            }

            if (lag > TimeSpan.FromSeconds(120))
            {
                return HealthCheckResult.Unhealthy($"Projection lag is significantly high: {lag.TotalSeconds:F0} seconds.");
            }

            if (lag > TimeSpan.FromSeconds(30))
            {
                return HealthCheckResult.Degraded($"Projection lag is moderate: {lag.TotalSeconds:F0} seconds.");
            }

            return HealthCheckResult.Healthy($"Projection lag is low: {lag.TotalSeconds:F0} seconds.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Failed to check projection lag: {ex.Message}");
        }
    }
}
