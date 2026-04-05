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
    public async Task PatientTrajectoryEndpoints_ShouldEnforceAuthorizationPolicies()
    {
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
        DateTime paymentAt,
        DateTime completedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var events = new DomainEvent[]
        {
            new PatientCheckedIn(queueId, patientId, patientId, null, 0, null, "corr-checkin")
            {
                OccurredAt = checkInAt
            },
            new PatientPaymentValidated(queueId, patientId, 15000m, turnId, "PAY-001", "corr-payment")
            {
                OccurredAt = paymentAt
            },
            new PatientAttentionCompleted(queueId, patientId, "ROOM-01", turnId, "Completed", "corr-complete")
            {
                OccurredAt = completedAt
            },
            new PatientAttentionCompleted("ROOM-01", patientId, "ROOM-01", turnId, "Completed", "corr-complete")
            {
                OccurredAt = completedAt.AddMilliseconds(1)
            }
        };

        await eventStore.SaveBatchAsync(events);
        await db.SaveChangesAsync();
    }
}
