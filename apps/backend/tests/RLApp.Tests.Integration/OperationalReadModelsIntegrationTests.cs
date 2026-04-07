using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Ports.Outbound;

namespace RLApp.Tests.Integration;

public class OperationalReadModelsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OperationalReadModelsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WaitingRoomMonitor_ShouldReturnProjectionBackedSnapshot_ForReceptionist()
    {
        var queueId = $"QUEUE-{Guid.NewGuid():N}";

        await SeedOperationalReadModelsAsync(queueId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/waiting-room/{queueId}/monitor");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("reception-user", "reception", "Receptionist"));
        request.Headers.Add("X-Correlation-Id", "corr-monitor-query");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("queueId").GetString().Should().Be(queueId);
        payload.GetProperty("waitingCount").GetInt32().Should().Be(2);
        payload.GetProperty("entries").GetArrayLength().Should().BeGreaterThanOrEqualTo(5);
        payload.GetProperty("statusBreakdown").EnumerateArray().Any(item => item.GetProperty("status").GetString() == "Waiting").Should().BeTrue();
        payload.GetProperty("statusBreakdown").EnumerateArray().Any(item => item.GetProperty("status").GetString() == "AtCashier").Should().BeTrue();
        payload.GetProperty("statusBreakdown").EnumerateArray().Any(item => item.GetProperty("status").GetString() == "WaitingForConsultation").Should().BeTrue();
    }

    [Fact]
    public async Task OperationsDashboard_ShouldReturnAggregatedSnapshot_ForSupervisor()
    {
        var queueId = $"QUEUE-{Guid.NewGuid():N}";

        await SeedOperationalReadModelsAsync(queueId);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/operations/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("supervisor-user", "supervisor", "Supervisor"));
        request.Headers.Add("X-Correlation-Id", "corr-dashboard-query");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("currentWaitingCount").GetInt32().Should().Be(2);
        payload.GetProperty("totalPatientsToday").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        payload.GetProperty("activeRooms").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        payload.GetProperty("queueSnapshots").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        payload.GetProperty("statusBreakdown").EnumerateArray().Any(item => item.GetProperty("status").GetString() == "InConsultation").Should().BeTrue();
        payload.GetProperty("statusBreakdown").EnumerateArray().Any(item => item.GetProperty("status").GetString() == "AtCashier").Should().BeTrue();
    }

    [Fact]
    public async Task OperationalReadModels_ShouldEnforceAuthorizationPolicies()
    {
        var queueId = $"QUEUE-{Guid.NewGuid():N}";

        var monitorUnauthorized = await _client.GetAsync($"/api/v1/waiting-room/{queueId}/monitor");
        monitorUnauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forbiddenMonitor = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/waiting-room/{queueId}/monitor");
        forbiddenMonitor.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("support-user", "support", "Support"));
        var forbiddenMonitorResponse = await _client.SendAsync(forbiddenMonitor);
        forbiddenMonitorResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var dashboardUnauthorized = await _client.GetAsync("/api/v1/operations/dashboard");
        dashboardUnauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forbiddenDashboard = new HttpRequestMessage(HttpMethod.Get, "/api/v1/operations/dashboard");
        forbiddenDashboard.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("reception-user", "reception", "Receptionist"));
        var forbiddenDashboardResponse = await _client.SendAsync(forbiddenDashboard);
        forbiddenDashboardResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OperationalCommandEndpoints_ShouldEnforceReceptionCashierAndSupervisorPolicies()
    {
        var queueId = $"QUEUE-{Guid.NewGuid():N}";

        var checkInPayload = new
        {
            queueId,
            appointmentReference = "APPT-001",
            patientId = "PAT-SEC-001",
            patientName = "Paciente Seguridad",
            consultationType = "General",
            priority = "1"
        };

        var receptionUnauthorized = await _client.PostAsJsonAsync("/api/waiting-room/check-in", checkInPayload);
        receptionUnauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forbiddenReception = new HttpRequestMessage(HttpMethod.Post, "/api/waiting-room/check-in")
        {
            Content = JsonContent.Create(checkInPayload)
        };
        forbiddenReception.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("cashier-user", "cashier", "Cashier"));
        forbiddenReception.Headers.Add("X-Correlation-Id", "corr-reception-forbidden");
        forbiddenReception.Headers.Add("X-Idempotency-Key", "idem-reception-forbidden");
        var forbiddenReceptionResponse = await _client.SendAsync(forbiddenReception);
        forbiddenReceptionResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var cashierPayload = new
        {
            queueId,
            cashierStationId = "CASH-01"
        };

        var cashierUnauthorized = await _client.PostAsJsonAsync("/api/cashier/call-next", cashierPayload);
        cashierUnauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forbiddenCashier = new HttpRequestMessage(HttpMethod.Post, "/api/cashier/call-next")
        {
            Content = JsonContent.Create(cashierPayload)
        };
        forbiddenCashier.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("reception-user", "reception", "Receptionist"));
        forbiddenCashier.Headers.Add("X-Correlation-Id", "corr-cashier-forbidden");
        forbiddenCashier.Headers.Add("X-Idempotency-Key", "idem-cashier-forbidden");
        var forbiddenCashierResponse = await _client.SendAsync(forbiddenCashier);
        forbiddenCashierResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var roomUnauthorized = await _client.PostAsJsonAsync("/api/medical/consulting-room/deactivate", new { roomId = "ROOM-SEC-001" });
        roomUnauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forbiddenRoom = new HttpRequestMessage(HttpMethod.Post, "/api/medical/consulting-room/deactivate")
        {
            Content = JsonContent.Create(new { roomId = "ROOM-SEC-001" })
        };
        forbiddenRoom.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("doctor-user", "doctor", "Doctor"));
        forbiddenRoom.Headers.Add("X-Correlation-Id", "corr-room-forbidden");
        var forbiddenRoomResponse = await _client.SendAsync(forbiddenRoom);
        forbiddenRoomResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task SeedOperationalReadModelsAsync(string queueId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingDashboard = await db.OperationsDashboards
            .SingleOrDefaultAsync(item => item.Id == "SYSTEM_SINGLETON");

        db.QueueStates.Add(new QueueStateView
        {
            QueueId = queueId,
            TotalPending = 2,
            AverageWaitTimeMinutes = 12.5,
            LastUpdatedAt = DateTime.UtcNow.AddSeconds(-5)
        });

        db.WaitingRoomMonitors.AddRange(
            new WaitingRoomMonitorView
            {
                TurnId = $"{queueId}-PAT-WAITING",
                QueueId = queueId,
                PatientId = "PAT-WAITING",
                PatientName = "Paciente Espera",
                TicketNumber = "R-001",
                Status = "Waiting",
                RoomAssigned = null,
                CheckedInAt = DateTime.UtcNow.AddMinutes(-20),
                UpdatedAt = DateTime.UtcNow.AddSeconds(-10)
            },
            new WaitingRoomMonitorView
            {
                TurnId = $"{queueId}-PAT-CASHIER",
                QueueId = queueId,
                PatientId = "PAT-CASHIER",
                PatientName = "Paciente Caja",
                TicketNumber = "R-001A",
                Status = "AtCashier",
                RoomAssigned = null,
                CheckedInAt = DateTime.UtcNow.AddMinutes(-18),
                UpdatedAt = DateTime.UtcNow.AddSeconds(-9)
            },
            new WaitingRoomMonitorView
            {
                TurnId = $"{queueId}-PAT-WAITING-CONSULTATION",
                QueueId = queueId,
                PatientId = "PAT-WAITING-CONSULTATION",
                PatientName = "Paciente Espera Consulta",
                TicketNumber = "R-001B",
                Status = "WaitingForConsultation",
                RoomAssigned = null,
                CheckedInAt = DateTime.UtcNow.AddMinutes(-17),
                UpdatedAt = DateTime.UtcNow.AddSeconds(-8)
            },
            new WaitingRoomMonitorView
            {
                TurnId = $"{queueId}-PAT-CONSULTATION",
                QueueId = queueId,
                PatientId = "PAT-CONSULTATION",
                PatientName = "Paciente Consulta",
                TicketNumber = "R-002",
                Status = "InConsultation",
                RoomAssigned = "ROOM-01",
                CheckedInAt = DateTime.UtcNow.AddMinutes(-35),
                UpdatedAt = DateTime.UtcNow.AddSeconds(-8)
            },
            new WaitingRoomMonitorView
            {
                TurnId = $"{queueId}-PAT-COMPLETED",
                QueueId = queueId,
                PatientId = "PAT-COMPLETED",
                PatientName = "Paciente Completado",
                TicketNumber = "R-003",
                Status = "Completed",
                RoomAssigned = "ROOM-02",
                CheckedInAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
            });

        if (existingDashboard is null)
        {
            db.OperationsDashboards.Add(new OperationsDashboardView
            {
                Id = "SYSTEM_SINGLETON",
                TotalPatientsToday = 5,
                ActiveRooms = 1,
                TotalCompleted = 1,
                Date = DateTime.UtcNow.Date
            });
        }
        else
        {
            existingDashboard.TotalPatientsToday = Math.Max(existingDashboard.TotalPatientsToday, 5);
            existingDashboard.ActiveRooms = Math.Max(existingDashboard.ActiveRooms, 1);
            existingDashboard.TotalCompleted = Math.Max(existingDashboard.TotalCompleted, 1);
            existingDashboard.Date = DateTime.UtcNow.Date;
        }

        await db.SaveChangesAsync();
    }

    private string CreateToken(string userId, string username, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        return jwtTokenService.GenerateToken(userId, username, role);
    }
}
