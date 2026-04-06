using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Application.Commands;

namespace RLApp.Tests.Integration;

public class LocalOutboxIntegrationTests : IClassFixture<LocalOutboxWebApplicationFactory>
{
    private readonly LocalOutboxWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LocalOutboxIntegrationTests(LocalOutboxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterPatientArrival_WhenExternalMessagingIsDisabled_ShouldMaterializeMonitorProjection()
    {
        const string queueId = "Q-LOCAL-OUTBOX-001";
        const string patientId = "PAT-LOCAL-OUTBOX-001";
        const string correlationId = "CORR-local-outbox-001";
        var turnId = $"{queueId}-{patientId}";

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new RegisterPatientArrivalCommand(
                queueId,
                patientId,
                "Paciente Local",
                null,
                1,
                null,
                correlationId,
                "reception-1"));

            result.Success.Should().BeTrue();
        }

        WaitingRoomMonitorView? projection = null;
        List<OutboxMessage>? outboxMessages = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var verificationScope = _factory.Services.CreateScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            projection = await db.WaitingRoomMonitors
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.TurnId == turnId);

            outboxMessages = await db.OutboxMessages
                .AsNoTracking()
                .Where(message => message.CorrelationId == correlationId)
                .OrderBy(message => message.OccurredAt)
                .ToListAsync();

            if (projection is not null && outboxMessages.Count == 4 && outboxMessages.All(message => message.ProcessedAt is not null))
            {
                break;
            }

            await Task.Delay(250);
        }

        projection.Should().NotBeNull();
        projection!.PatientName.Should().Be("Paciente Local");
        projection.QueueId.Should().Be(queueId);
        projection.PatientId.Should().Be(patientId);
        projection.TicketNumber.Should().Be(turnId);
        projection.Status.Should().Be("Waiting");
        projection.CheckedInAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-2));

        outboxMessages.Should().NotBeNull();
        outboxMessages!.Should().HaveCount(4);
        outboxMessages.Should().OnlyContain(message => message.ProcessedAt != null);
        outboxMessages.Select(message => message.Type).Should().BeEquivalentTo(new[]
        {
            "WaitingQueueCreated",
            "PatientTrajectoryOpened",
            "PatientTrajectoryStageRecorded",
            "PatientCheckedIn"
        });
    }

    [Fact]
    public async Task HealthCheck_WhenExternalMessagingIsDisabled_ShouldRemainHealthy()
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

        var content = JsonSerializer.Deserialize<JsonElement>(detail!);
        content.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task UnknownOutboxMessage_WhenExternalMessagingIsDisabled_ShouldMoveToDeadLetterStorage()
    {
        const string correlationId = "CORR-local-deadletter-001";
        Guid messageId;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = "Q-LOCAL-DEADLETTER-001",
                CorrelationId = correlationId,
                Type = "UnknownOutboxEvent",
                Payload = "{}",
                OccurredAt = DateTime.UtcNow.AddSeconds(-1)
            };

            messageId = message.Id;
            db.OutboxMessages.Add(message);
            await db.SaveChangesAsync();
        }

        OutboxDeadLetterMessage? deadLetter = null;
        var outboxMessageStillPending = true;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            deadLetter = await db.OutboxDeadLetterMessages
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == messageId);

            outboxMessageStillPending = await db.OutboxMessages
                .AsNoTracking()
                .AnyAsync(item => item.Id == messageId);

            if (deadLetter is not null && !outboxMessageStillPending)
            {
                break;
            }

            await Task.Delay(250);
        }

        deadLetter.Should().NotBeNull();
        deadLetter!.CorrelationId.Should().Be(correlationId);
        deadLetter.Type.Should().Be("UnknownOutboxEvent");
        deadLetter.FailureReason.Should().Contain("Unknown event type");
        outboxMessageStillPending.Should().BeFalse();
    }
}
