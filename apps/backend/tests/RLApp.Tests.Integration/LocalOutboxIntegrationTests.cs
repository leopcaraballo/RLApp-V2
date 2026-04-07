using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Application.Commands;
using RLApp.Ports.Outbound;

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
    public async Task CashierFlow_WhenExternalMessagingIsDisabled_ShouldAdvanceVisibleMonitorStates()
    {
        const string queueId = "Q-LOCAL-OUTBOX-CASHIER-001";
        const string patientId = "PAT-LOCAL-OUTBOX-CASHIER-001";
        const string userId = "cashier-1";
        var turnId = $"{queueId}-{patientId}";

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var registerResult = await mediator.Send(new RegisterPatientArrivalCommand(
                queueId,
                patientId,
                "Paciente Caja Local",
                null,
                1,
                null,
                "CORR-local-cashier-register",
                "reception-1"));

            registerResult.Success.Should().BeTrue();

            var callResult = await mediator.Send(new CallNextAtCashierCommand(
                queueId,
                "CASH-01",
                "CORR-local-cashier-call",
                userId));

            callResult.Success.Should().BeTrue();

            await WaitForMonitorStatusAsync(turnId, OperationalVisibleStatuses.AtCashier);

            var pendingResult = await mediator.Send(new MarkPaymentPendingCommand(
                queueId,
                patientId,
                "CORR-local-cashier-pending",
                userId));

            pendingResult.Success.Should().BeTrue();

            await WaitForMonitorStatusAsync(turnId, OperationalVisibleStatuses.PaymentPending);

            var validateResult = await mediator.Send(new ValidatePaymentCommand(
                queueId,
                patientId,
                25m,
                turnId,
                "PAY-LOCAL-001",
                "CORR-local-cashier-validated",
                userId));

            validateResult.Success.Should().BeTrue();
        }

        await WaitForMonitorStatusAsync(turnId, OperationalVisibleStatuses.WaitingForConsultation);
    }

    [Fact]
    public async Task CashierAbsence_WhenExternalMessagingIsDisabled_ShouldMaterializeAbsentMonitorStatus()
    {
        const string queueId = "Q-LOCAL-OUTBOX-CASHIER-ABSENT-001";
        const string patientId = "PAT-LOCAL-OUTBOX-CASHIER-ABSENT-001";
        const string userId = "cashier-1";
        const string absenceCorrelationId = "CORR-local-cashier-absent-mark";
        var turnId = $"{queueId}-{patientId}";

        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var registerResult = await mediator.Send(new RegisterPatientArrivalCommand(
                queueId,
                patientId,
                "Paciente Caja Ausente",
                null,
                1,
                null,
                "CORR-local-cashier-absent-register",
                "reception-1"));

            registerResult.Success.Should().BeTrue();

            var callResult = await mediator.Send(new CallNextAtCashierCommand(
                queueId,
                "CASH-01",
                "CORR-local-cashier-absent-call",
                userId));

            callResult.Success.Should().BeTrue();

            await WaitForMonitorStatusAsync(turnId, OperationalVisibleStatuses.AtCashier);

            var pendingResult = await mediator.Send(new MarkPaymentPendingCommand(
                queueId,
                patientId,
                "CORR-local-cashier-absent-pending",
                userId));

            pendingResult.Success.Should().BeTrue();

            await WaitForMonitorStatusAsync(turnId, OperationalVisibleStatuses.PaymentPending);

            var absentResult = await mediator.Send(new MarkAbsenceCommand(
                queueId,
                patientId,
                "ROOM-CASHIER",
                turnId,
                "cashier-no-show",
                absenceCorrelationId,
                userId));

            absentResult.Success.Should().BeTrue();
        }

        await WaitForMonitorStatusAsync(turnId, OperationalVisibleStatuses.Absent);

        PatientTrajectoryView? trajectoryProjection = null;
        OutboxMessage? cancelledTrajectoryMessage = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            trajectoryProjection = await db.PatientTrajectories
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.PatientId == patientId && item.QueueId == queueId);

            cancelledTrajectoryMessage = await db.OutboxMessages
                .AsNoTracking()
                .Where(message => message.CorrelationId == absenceCorrelationId && message.Type == "PatientTrajectoryCancelled")
                .OrderByDescending(message => message.OccurredAt)
                .FirstOrDefaultAsync();

            if (trajectoryProjection?.CurrentState == "TrayectoriaCancelada" && cancelledTrajectoryMessage?.ProcessedAt is not null)
            {
                break;
            }

            await Task.Delay(250);
        }

        trajectoryProjection.Should().NotBeNull();
        trajectoryProjection!.CurrentState.Should().Be("TrayectoriaCancelada");

        cancelledTrajectoryMessage.Should().NotBeNull();
        cancelledTrajectoryMessage!.ProcessedAt.Should().NotBeNull();

        var cancelledPayload = JsonSerializer.Deserialize<JsonElement>(cancelledTrajectoryMessage.Payload);
        cancelledPayload.GetProperty("sourceEvent").GetString().Should().Be("PatientAbsentAtCashier");
        cancelledPayload.GetProperty("sourceState").GetString().Should().Be("CanceladoPorAusencia");
        cancelledPayload.GetProperty("reason").GetString().Should().Be("cashier-no-show");
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

    private async Task WaitForMonitorStatusAsync(string turnId, string expectedStatus)
    {
        WaitingRoomMonitorView? projection = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            projection = await db.WaitingRoomMonitors
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.TurnId == turnId);

            if (projection is not null && projection.Status == expectedStatus)
            {
                break;
            }

            await Task.Delay(250);
        }

        projection.Should().NotBeNull();
        projection!.Status.Should().Be(expectedStatus);
    }
}
