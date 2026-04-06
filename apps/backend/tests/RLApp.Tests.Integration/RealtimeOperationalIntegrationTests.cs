using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Infrastructure.Realtime;

namespace RLApp.Tests.Integration;

public class RealtimeOperationalIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RealtimeOperationalIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MetricsEndpoint_ShouldExposeRealtimeMetrics_AfterChannelActivityIsRecorded()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var realtimeChannelStatus = scope.ServiceProvider.GetRequiredService<RealtimeChannelStatus>();
            realtimeChannelStatus.RecordConnectionOpened();
            realtimeChannelStatus.RecordPublishSucceeded("PatientCheckedIn", "all", TimeSpan.FromMilliseconds(12));
        }

        string? readyPayload = null;
        string? metricsPayload = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var readyResponse = await _client.GetAsync("/health/ready");
            readyPayload = await readyResponse.Content.ReadAsStringAsync();

            var metricsResponse = await _client.GetAsync("/metrics");
            metricsPayload = await metricsResponse.Content.ReadAsStringAsync();

            if (readyResponse.IsSuccessStatusCode
                && readyPayload.Contains("RealtimeChannel", StringComparison.Ordinal)
                && metricsPayload.Contains("rlapp_realtime_publications", StringComparison.Ordinal)
                && metricsPayload.Contains("rlapp_realtime_publication_duration_ms", StringComparison.Ordinal))
            {
                break;
            }

            await Task.Delay(250);
        }

        var finalMetricsResponse = await _client.GetAsync("/metrics");
        finalMetricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        metricsPayload.Should().NotBeNull();
        metricsPayload!.Should().Contain("rlapp_realtime_publications");
        metricsPayload.Should().Contain("rlapp_realtime_publication_duration_ms");

        readyPayload.Should().NotBeNull();
        var readyDocument = JsonSerializer.Deserialize<JsonElement>(readyPayload!);
        readyDocument.GetProperty("details")
            .EnumerateArray()
            .Any(item => item.GetProperty("key").GetString() == "RealtimeChannel"
                && item.GetProperty("status").GetString() == "Healthy")
            .Should().BeTrue();
    }
}
