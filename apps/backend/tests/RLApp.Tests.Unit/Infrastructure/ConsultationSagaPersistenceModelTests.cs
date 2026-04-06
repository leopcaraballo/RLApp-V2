namespace RLApp.Tests.Unit.Infrastructure;

using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Messaging.Sagas;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Configurations;

public class ConsultationSagaPersistenceModelTests
{
    [Fact]
    public void AppDbContext_ShouldMapConsultationSagaStateWithExpectedIndexes()
    {
        using var context = CreateContext($"consultation-saga-model-{Guid.NewGuid()}");

        var entityType = context.Model.FindEntityType(typeof(ConsultationState));

        Assert.NotNull(entityType);
        Assert.Equal("ConsultationSagaStates", entityType!.GetTableName());

        var trajectoryIndex = entityType.GetIndexes()
            .Single(index => index.GetDatabaseName() == ConsultationStateConfiguration.TrajectoryIdIndexName);
        var lastCorrelationIndex = entityType.GetIndexes()
            .Single(index => index.GetDatabaseName() == ConsultationStateConfiguration.LastCorrelationIdIndexName);

        Assert.Equal(nameof(ConsultationState.TrajectoryId), trajectoryIndex.Properties.Single().Name);
        Assert.Equal(nameof(ConsultationState.LastCorrelationId), lastCorrelationIndex.Properties.Single().Name);
    }

    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options);
    }
}
