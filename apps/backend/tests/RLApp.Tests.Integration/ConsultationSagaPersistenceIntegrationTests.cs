using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Messaging.Sagas;
using RLApp.Adapters.Persistence.Data;

namespace RLApp.Tests.Integration;

public class ConsultationSagaPersistenceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConsultationSagaPersistenceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConsultationSagaState_ShouldPersistInPostgres()
    {
        var correlationId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.ConsultationSagaStates.Add(new ConsultationState
            {
                CorrelationId = correlationId,
                CurrentState = "WaitingForPatient",
                TrajectoryId = "TRJ-Q-01-PAT-01-20260406090000000",
                LastCorrelationId = "CORR-consultation-saga-001",
                PatientId = "PAT-01",
                QueueId = "Q-01",
                RoomId = "ROOM-01",
                CalledAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        using (var verificationScope = _factory.Services.CreateScope())
        {
            var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var persisted = await db.ConsultationSagaStates
                .AsNoTracking()
                .SingleAsync(state => state.CorrelationId == correlationId);

            persisted.TrajectoryId.Should().Be("TRJ-Q-01-PAT-01-20260406090000000");
            persisted.LastCorrelationId.Should().Be("CORR-consultation-saga-001");
            persisted.CurrentState.Should().Be("WaitingForPatient");
        }
    }
}
