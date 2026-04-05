using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Application.Commands;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

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
            new RegisterPatientArrivalCommand(queueId, "PAT-001", "Ana Perez", null, 1, null, correlationId, "reception-1"));

        result.Success.Should().BeTrue();

        var events = await db.EventStore
            .AsNoTracking()
            .Where(e => e.AggregateId == queueId && e.CorrelationId == correlationId)
            .OrderBy(e => e.SequenceNumber)
            .Select(e => new { e.EventType, e.SequenceNumber })
            .ToListAsync();

        events.Select(item => item.EventType).Should().ContainInOrder("WaitingQueueCreated", "PatientCheckedIn");
        events.Select(item => item.SequenceNumber).Should().ContainInOrder(1, 2);

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
            null,
            1,
            null,
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

    [Fact]
    public async Task SaveChangesAsync_WhenConcurrentQueueWritersReuseSameExpectedVersion_ShouldRaiseConflictAndRollbackSecondWriter()
    {
        const string queueId = "Q-CONFLICT-001";
        const string seedCorrelationId = "CORR-conflict-seed-001";
        const string winningCorrelationId = "CORR-conflict-win-001";
        const string conflictingCorrelationId = "CORR-conflict-lose-001";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var mediator = seedScope.ServiceProvider.GetRequiredService<IMediator>();

            var seedResult = await mediator.Send(new RegisterPatientArrivalCommand(
                queueId,
                "PAT-CONFLICT-001",
                "Paciente Semilla",
                null,
                1,
                null,
                seedCorrelationId,
                "reception-1"));

            seedResult.Success.Should().BeTrue();
        }

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();

        var queueRepositoryA = scopeA.ServiceProvider.GetRequiredService<IWaitingQueueRepository>();
        var queueRepositoryB = scopeB.ServiceProvider.GetRequiredService<IWaitingQueueRepository>();
        var persistenceSessionA = scopeA.ServiceProvider.GetRequiredService<IPersistenceSession>();
        var persistenceSessionB = scopeB.ServiceProvider.GetRequiredService<IPersistenceSession>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();

        var queueA = await queueRepositoryA.GetByIdAsync(queueId);
        var queueB = await queueRepositoryB.GetByIdAsync(queueId);

        queueA.Version.Should().Be(2);
        queueB.Version.Should().Be(2);

        queueA.CheckInPatient("PAT-CONFLICT-002", "Paciente Ganador", null, 1, null, winningCorrelationId);
        await queueRepositoryA.UpdateAsync(queueA);

        queueB.CheckInPatient("PAT-CONFLICT-003", "Paciente Conflicto", null, 1, null, conflictingCorrelationId);
        await queueRepositoryB.UpdateAsync(queueB);

        dbB.OutboxMessages.Add(new OutboxMessage
        {
            AggregateId = queueId,
            CorrelationId = conflictingCorrelationId,
            Type = "PatientCheckedIn",
            Payload = "{}",
            OccurredAt = DateTime.UtcNow
        });

        dbB.AuditLogs.Add(new AuditLogRecord
        {
            Actor = "integration-test",
            Action = "REGISTER_PATIENT_ARRIVAL",
            Entity = "WaitingQueue",
            EntityId = queueId,
            Payload = "{}",
            CorrelationId = conflictingCorrelationId,
            Success = true,
            OccurredAt = DateTime.UtcNow
        });

        await persistenceSessionA.SaveChangesAsync();

        var act = async () => await persistenceSessionB.SaveChangesAsync();

        var thrown = await act.Should().ThrowAsync<DomainException>();
        thrown.Which.Code.Should().Be(DomainException.ConcurrencyConflictCode);
        thrown.Which.Message.Should().Contain(queueId);

        using var verificationScope = _factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var persistedQueueEvents = await db.EventStore
            .AsNoTracking()
            .Where(e => e.AggregateId == queueId)
            .OrderBy(e => e.SequenceNumber)
            .Select(e => new { e.CorrelationId, e.EventType, e.SequenceNumber })
            .ToListAsync();

        persistedQueueEvents.Select(item => item.SequenceNumber).Should().ContainInOrder(1, 2, 3);
        persistedQueueEvents.Should().ContainSingle(item => item.CorrelationId == winningCorrelationId && item.SequenceNumber == 3);
        persistedQueueEvents.Should().NotContain(item => item.CorrelationId == conflictingCorrelationId);

        var outboxMessages = await db.OutboxMessages
            .AsNoTracking()
            .Where(message => message.CorrelationId == conflictingCorrelationId)
            .ToListAsync();

        outboxMessages.Should().BeEmpty();

        var auditLogs = await db.AuditLogs
            .AsNoTracking()
            .Where(log => log.CorrelationId == conflictingCorrelationId)
            .ToListAsync();

        auditLogs.Should().BeEmpty();
    }
}
