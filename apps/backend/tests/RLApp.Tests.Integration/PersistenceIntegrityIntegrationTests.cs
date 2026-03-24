using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data;
using RLApp.Application.Commands;

namespace RLApp.Tests.Integration;

public class PersistenceIntegrityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PersistenceIntegrityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterPatientArrival_ShouldPersistEventsOutboxAndAudit_InSingleOperation()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var correlationId = "CORR-persist-success-001";
        var queueId = "Q-PERSIST-001";

        var result = await mediator.Send(
            new RegisterPatientArrivalCommand(queueId, "PAT-001", "Ana Perez", correlationId, "reception-1"));

        result.Success.Should().BeTrue();

        var events = await db.EventStore
            .AsNoTracking()
            .Where(e => e.AggregateId == queueId && e.CorrelationId == correlationId)
            .OrderBy(e => e.OccurredAt)
            .Select(e => e.EventType)
            .ToListAsync();

        events.Should().ContainInOrder("WaitingQueueCreated", "PatientCheckedIn");

        var outboxMessages = await db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.AggregateId == queueId && m.CorrelationId == correlationId)
            .ToListAsync();

        outboxMessages.Should().HaveCount(2);

        var auditLog = await db.AuditLogs
            .AsNoTracking()
            .SingleAsync(a => a.CorrelationId == correlationId);

        auditLog.Action.Should().Be("REGISTER_PATIENT_ARRIVAL");
        auditLog.Success.Should().BeTrue();
        auditLog.Entity.Should().Be("WaitingQueue");
        auditLog.EntityId.Should().Be(queueId);
    }

    [Fact]
    public async Task ClaimNextPatient_WhenRoomDoesNotExist_ShouldNotPersistPartialQueueMutation()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        const string queueId = "Q-PERSIST-ROLLBACK-001";
        await mediator.Send(new RegisterPatientArrivalCommand(
            queueId,
            "PAT-ROLLBACK-001",
            "Paciente Rollback",
            "CORR-seed-rollback-001",
            "reception-1"));

        var failureCorrelationId = "CORR-claim-failure-001";
        var result = await mediator.Send(new ClaimNextPatientCommand(
            queueId,
            "ROOM-MISSING-001",
            failureCorrelationId,
            "doctor-1"));

        result.Success.Should().BeFalse();

        var claimEvents = await db.EventStore
            .AsNoTracking()
            .Where(e =>
                e.AggregateId == queueId &&
                e.CorrelationId == failureCorrelationId &&
                e.EventType == "PatientClaimedForAttention")
            .ToListAsync();

        claimEvents.Should().BeEmpty();

        var outboxMessages = await db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.AggregateId == queueId && m.CorrelationId == failureCorrelationId)
            .ToListAsync();

        outboxMessages.Should().BeEmpty();

        var failureAudit = await db.AuditLogs
            .AsNoTracking()
            .SingleAsync(a => a.CorrelationId == failureCorrelationId);

        failureAudit.Action.Should().Be("CLAIM_NEXT_PATIENT");
        failureAudit.Success.Should().BeFalse();
        failureAudit.EntityId.Should().Be(queueId);
    }
}
