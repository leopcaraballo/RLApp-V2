using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Tests.Integration;

public class PatientTrajectoryIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PatientTrajectoryIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rebuild_ShouldMaterializeTrajectoryProjection_AndQueryShouldReturnChronologicalStages()
    {
        var queueId = $"QUEUE-{Guid.NewGuid():N}";
        var patientId = $"PAT-{Guid.NewGuid():N}";
        var turnId = $"{queueId}-{patientId}";
        var checkInAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc);
        var paymentAt = checkInAt.AddMinutes(12);
        var completedAt = checkInAt.AddMinutes(32);
        var trajectoryId = PatientTrajectoryIdFactory.Create(queueId, patientId, checkInAt);

        await SeedHistoricalEventsAsync(queueId, patientId, turnId, checkInAt, paymentAt, completedAt);

        var rebuildRequest = new HttpRequestMessage(HttpMethod.Post, "/api/patient-trajectories/rebuild")
        {
            Content = JsonContent.Create(new
            {
                queueId,
                patientId,
                dryRun = false
            })
        };
        rebuildRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("support-user", "support", "Support"));
        rebuildRequest.Headers.Add("X-Correlation-Id", "corr-rebuild");
        rebuildRequest.Headers.Add("X-Idempotency-Key", $"idem-{Guid.NewGuid():N}");

        var rebuildResponse = await _client.SendAsync(rebuildRequest);
        rebuildResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storedTrajectory = await db.PatientTrajectories
                .AsNoTracking()
                .FirstOrDefaultAsync(trajectory => trajectory.TrajectoryId == trajectoryId);

            storedTrajectory.Should().NotBeNull();
            storedTrajectory!.CurrentState.Should().Be("TrayectoriaFinalizada");
        }

        var queryRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/patient-trajectories/{trajectoryId}");
        queryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("supervisor-user", "supervisor", "Supervisor"));
        queryRequest.Headers.Add("X-Correlation-Id", "corr-query");

        var queryResponse = await _client.SendAsync(queryRequest);
        queryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await queryResponse.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("trajectoryId").GetString().Should().Be(trajectoryId);
        payload.GetProperty("patientId").GetString().Should().Be(patientId);
        payload.GetProperty("queueId").GetString().Should().Be(queueId);
        payload.GetProperty("currentState").GetString().Should().Be("TrayectoriaFinalizada");
        payload.GetProperty("stages").GetArrayLength().Should().Be(3);
        payload.GetProperty("stages")[0].GetProperty("stage").GetString().Should().Be("Recepcion");
        payload.GetProperty("stages")[1].GetProperty("stage").GetString().Should().Be("Caja");
        payload.GetProperty("stages")[2].GetProperty("stage").GetString().Should().Be("Consulta");
    }

    [Fact]
    public async Task Discover_ShouldReturnOrderedTrajectoryCandidates_AndAllowQueueFiltering()
    {
        var patientId = $"PAT-{Guid.NewGuid():N}";
        var closedQueueId = $"QUEUE-CLOSED-{Guid.NewGuid():N}";
        var activeQueueId = $"QUEUE-ACTIVE-{Guid.NewGuid():N}";

        var closedCheckInAt = new DateTime(2026, 4, 2, 8, 30, 0, DateTimeKind.Utc);
        var closedPaymentAt = closedCheckInAt.AddMinutes(8);
        var closedCompletedAt = closedCheckInAt.AddMinutes(27);
        var activeCheckInAt = closedCheckInAt.AddDays(1);

        await SeedHistoricalEventsAsync(
            closedQueueId,
            patientId,
            $"{closedQueueId}-{patientId}",
            closedCheckInAt,
            closedPaymentAt,
            closedCompletedAt);

        await SeedHistoricalEventsAsync(
            activeQueueId,
            patientId,
            $"{activeQueueId}-{patientId}",
            activeCheckInAt,
            paymentAt: null,
            completedAt: null);

        var rebuildRequest = new HttpRequestMessage(HttpMethod.Post, "/api/patient-trajectories/rebuild")
        {
            Content = JsonContent.Create(new
            {
                patientId,
                dryRun = false
            })
        };
        rebuildRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("support-user", "support", "Support"));
        rebuildRequest.Headers.Add("X-Correlation-Id", "corr-discovery-rebuild");
        rebuildRequest.Headers.Add("X-Idempotency-Key", $"idem-{Guid.NewGuid():N}");

        var rebuildResponse = await _client.SendAsync(rebuildRequest);
        rebuildResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var discoveryRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/patient-trajectories?patientId={Uri.EscapeDataString(patientId)}");
        discoveryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("supervisor-user", "supervisor", "Supervisor"));
        discoveryRequest.Headers.Add("X-Correlation-Id", "corr-discovery");

        var discoveryResponse = await _client.SendAsync(discoveryRequest);
        discoveryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var discoveryPayload = await discoveryResponse.Content.ReadFromJsonAsync<JsonElement>();
        discoveryPayload.GetProperty("total").GetInt32().Should().Be(2);
        discoveryPayload.GetProperty("items")[0].GetProperty("queueId").GetString().Should().Be(activeQueueId);
        discoveryPayload.GetProperty("items")[0].GetProperty("currentState").GetString().Should().Be("TrayectoriaActiva");
        discoveryPayload.GetProperty("items")[1].GetProperty("queueId").GetString().Should().Be(closedQueueId);
        discoveryPayload.GetProperty("items")[1].GetProperty("currentState").GetString().Should().Be("TrayectoriaFinalizada");

        var filteredRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/patient-trajectories?patientId={Uri.EscapeDataString(patientId)}&queueId={Uri.EscapeDataString(closedQueueId)}");
        filteredRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("supervisor-user", "supervisor", "Supervisor"));
        filteredRequest.Headers.Add("X-Correlation-Id", "corr-discovery-filtered");

        var filteredResponse = await _client.SendAsync(filteredRequest);
        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var filteredPayload = await filteredResponse.Content.ReadFromJsonAsync<JsonElement>();
        filteredPayload.GetProperty("total").GetInt32().Should().Be(1);
        filteredPayload.GetProperty("items")[0].GetProperty("queueId").GetString().Should().Be(closedQueueId);
    }

    [Fact]
    public async Task Discover_ShouldRejectRequestsWithoutPatientId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/patient-trajectories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("supervisor-user", "supervisor", "Supervisor"));
        request.Headers.Add("X-Correlation-Id", "corr-discovery-invalid");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().Should().Be("TRAJECTORY_DISCOVERY_SCOPE_INVALID");
    }

    [Fact]
    public async Task Discover_ShouldExposePrometheusMetrics()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/patient-trajectories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("supervisor-user", "supervisor", "Supervisor"));
        request.Headers.Add("X-Correlation-Id", "corr-discovery-metrics");

        var discoveryResponse = await _client.SendAsync(request);
        discoveryResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var metricsResponse = await _client.GetAsync("/metrics");
        metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var metricsPayload = await metricsResponse.Content.ReadAsStringAsync();
        metricsPayload.Should().Contain("rlapp_patient_trajectory_discovery_requests");
        metricsPayload.Should().Contain("rlapp_patient_trajectory_discovery_duration_ms");
        metricsPayload.Should().Contain("rlapp_patient_trajectory_discovery_match_count");
    }

    [Fact]
    public async Task PatientTrajectoryEndpoints_ShouldEnforceAuthorizationPolicies()
    {
        var discoveryResponse = await _client.GetAsync($"/api/patient-trajectories?patientId=PAT-{Guid.NewGuid():N}");
        discoveryResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forbiddenDiscovery = new HttpRequestMessage(HttpMethod.Get, $"/api/patient-trajectories?patientId=PAT-{Guid.NewGuid():N}");
        forbiddenDiscovery.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("cashier-user", "cashier", "Cashier"));
        var forbiddenDiscoveryResponse = await _client.SendAsync(forbiddenDiscovery);
        forbiddenDiscoveryResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var queryResponse = await _client.GetAsync($"/api/patient-trajectories/TRJ-{Guid.NewGuid():N}");
        queryResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forbiddenQuery = new HttpRequestMessage(HttpMethod.Get, $"/api/patient-trajectories/TRJ-{Guid.NewGuid():N}");
        forbiddenQuery.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("cashier-user", "cashier", "Cashier"));
        var forbiddenQueryResponse = await _client.SendAsync(forbiddenQuery);
        forbiddenQueryResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var rebuildRequest = new HttpRequestMessage(HttpMethod.Post, "/api/patient-trajectories/rebuild")
        {
            Content = JsonContent.Create(new { dryRun = true })
        };
        rebuildRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("supervisor-user", "supervisor", "Supervisor"));
        rebuildRequest.Headers.Add("X-Correlation-Id", "corr-forbidden");
        rebuildRequest.Headers.Add("X-Idempotency-Key", $"idem-{Guid.NewGuid():N}");

        var rebuildResponse = await _client.SendAsync(rebuildRequest);
        rebuildResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private string CreateToken(string userId, string username, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtTokenService.GenerateToken(userId, username, role);
    }

    private async Task SeedHistoricalEventsAsync(
        string queueId,
        string patientId,
        string turnId,
        DateTime checkInAt,
        DateTime? paymentAt,
        DateTime? completedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var trajectoryId = PatientTrajectoryIdFactory.Create(queueId, patientId, checkInAt);

        var events = new List<DomainEvent>
        {
            new PatientCheckedIn(queueId, patientId, patientId, null, 0, null, $"corr-checkin-{queueId}")
            {
                OccurredAt = checkInAt
            }
        };

        if (paymentAt.HasValue)
        {
            events.Add(new PatientPaymentValidated(queueId, patientId, 15000m, turnId, "PAY-001", $"corr-payment-{queueId}")
            {
                OccurredAt = paymentAt.Value
            });
        }

        if (completedAt.HasValue)
        {
            events.Add(new PatientAttentionCompleted(queueId, patientId, "ROOM-01", turnId, "Completed", $"corr-complete-{queueId}", trajectoryId)
            {
                OccurredAt = completedAt.Value
            });
            events.Add(new PatientAttentionCompleted("ROOM-01", patientId, "ROOM-01", turnId, "Completed", $"corr-complete-{queueId}", trajectoryId)
            {
                OccurredAt = completedAt.Value.AddMilliseconds(1)
            });
        }

        await eventStore.SaveBatchAsync(events);
        await db.SaveChangesAsync();
    }
}
