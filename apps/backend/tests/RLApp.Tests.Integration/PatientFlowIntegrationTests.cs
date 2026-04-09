using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Infrastructure.Security;

namespace RLApp.Tests.Integration;

public class PatientFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string IntegrationPassword = "local-integration-pass";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PatientFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Authentication_WithValidCredentials_ShouldSucceed()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hashService = new Pbkdf2PasswordHashService();

            db.StaffUsers.Add(new StaffUserRecord
            {
                Id = "staff-admin",
                Username = "admin",
                Email = "admin@clinic.local",
                PasswordHash = hashService.HashPassword(IntegrationPassword),
                Role = "Supervisor",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var loginRequest = new { identifier = "admin", password = IntegrationPassword };
        var response = await _client.PostAsJsonAsync("/api/staff/auth/login", loginRequest);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("username").GetString().Should().Be("admin");
    }

    [Fact]
    public async Task HealthCheck_ShouldBeHealthy()
    {
        HttpResponseMessage? response = null;
        string? detail = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            response = await _client.GetAsync("/health/ready");
            detail = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                break;
            }

            await Task.Delay(250);
        }

        response.Should().NotBeNull();

        if (response is null || !response.IsSuccessStatusCode)
        {
            throw new Exception($"Health check failed with {response?.StatusCode}. Detail: {detail}");
        }

        response.EnsureSuccessStatusCode();
        var content = JsonSerializer.Deserialize<JsonElement>(detail!);
        content.GetProperty("status").GetString().Should().Be("Healthy");
        content.GetProperty("details")
            .EnumerateArray()
            .Any(item => item.GetProperty("key").GetString() == "ProjectionLag")
            .Should().BeTrue();
        content.GetProperty("details")
            .EnumerateArray()
            .Any(item => item.GetProperty("key").GetString() == "RealtimeChannel")
            .Should().BeTrue();
    }
}
