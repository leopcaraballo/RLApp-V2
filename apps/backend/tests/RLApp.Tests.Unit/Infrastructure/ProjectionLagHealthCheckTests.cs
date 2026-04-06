using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Infrastructure.HealthChecks;

namespace RLApp.Tests.Unit.Infrastructure;

public class ProjectionLagHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenNoEventsExist_ShouldReturnHealthy()
    {
        await using var context = CreateContext();
        var healthCheck = new ProjectionLagHealthCheck(context);

        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenProjectionLagExceedsThirtySeconds_ShouldReturnDegraded()
    {
        var now = DateTime.UtcNow;

        await using var context = CreateContext();
        context.EventStore.Add(new EventRecord
        {
            AggregateId = "queue-1",
            SequenceNumber = 1,
            EventType = "PatientCheckedIn",
            CorrelationId = "corr-1",
            Payload = "{}",
            OccurredAt = now
        });
        context.QueueStates.Add(new QueueStateView
        {
            QueueId = "queue-1",
            TotalPending = 1,
            AverageWaitTimeMinutes = 2,
            LastUpdatedAt = now.AddSeconds(-45)
        });
        await context.SaveChangesAsync();

        var healthCheck = new ProjectionLagHealthCheck(context);
        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenProjectionLagExceedsOneHundredTwentySeconds_ShouldReturnUnhealthy()
    {
        var now = DateTime.UtcNow;

        await using var context = CreateContext();
        context.EventStore.Add(new EventRecord
        {
            AggregateId = "queue-1",
            SequenceNumber = 1,
            EventType = "PatientCheckedIn",
            CorrelationId = "corr-1",
            Payload = "{}",
            OccurredAt = now
        });
        context.WaitingRoomMonitors.Add(new WaitingRoomMonitorView
        {
            TurnId = "turn-1",
            PatientName = "Paciente 1",
            TicketNumber = "A-001",
            Status = "Waiting",
            UpdatedAt = now.AddSeconds(-180)
        });
        await context.SaveChangesAsync();

        var healthCheck = new ProjectionLagHealthCheck(context);
        var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, result.Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"projection-lag-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }
}
