using Microsoft.Extensions.Diagnostics.HealthChecks;
using RLApp.Infrastructure.HealthChecks;
using RLApp.Infrastructure.Realtime;

namespace RLApp.Tests.Unit.Infrastructure;

public class RealtimeChannelHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenNoActiveConnectionsExist_ShouldReturnHealthy()
    {
        var status = new RealtimeChannelStatus();
        var healthCheck = new RealtimeChannelHealthCheck(status);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionsExistAndNoPublicationsHaveBeenRecorded_ShouldReturnHealthy()
    {
        var status = new RealtimeChannelStatus();
        status.RecordConnectionOpened();
        var healthCheck = new RealtimeChannelHealthCheck(status);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenLatestPublicationFailedWithActiveConnections_ShouldReturnDegraded()
    {
        var status = new RealtimeChannelStatus();
        status.RecordConnectionOpened();
        status.RecordPublishFailed("PatientCheckedIn", "all", TimeSpan.FromMilliseconds(15), new InvalidOperationException("socket closed"));
        var healthCheck = new RealtimeChannelHealthCheck(status);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPublicationRecoversAfterFailure_ShouldReturnHealthy()
    {
        var status = new RealtimeChannelStatus();
        status.RecordConnectionOpened();
        status.RecordPublishFailed("PatientCheckedIn", "all", TimeSpan.FromMilliseconds(15), new InvalidOperationException("socket closed"));
        status.RecordPublishSucceeded("PatientCheckedIn", "all", TimeSpan.FromMilliseconds(10));
        var healthCheck = new RealtimeChannelHealthCheck(status);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
