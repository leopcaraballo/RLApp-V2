using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RLApp.Adapters.Messaging.Sagas;

namespace RLApp.Adapters.Persistence.Data.Configurations;

public sealed class ConsultationStateConfiguration : IEntityTypeConfiguration<ConsultationState>
{
    public const string TrajectoryIdIndexName = "IX_ConsultationSagaStates_TrajectoryId";
    public const string LastCorrelationIdIndexName = "IX_ConsultationSagaStates_LastCorrelationId";

    public void Configure(EntityTypeBuilder<ConsultationState> builder)
    {
        builder.ToTable("ConsultationSagaStates");

        builder.HasKey(state => state.CorrelationId);
        builder.Property(state => state.CorrelationId).ValueGeneratedNever();

        builder.Property(state => state.CurrentState)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(state => state.TrajectoryId)
            .HasMaxLength(128);

        builder.Property(state => state.LastCorrelationId)
            .HasMaxLength(128);

        builder.Property(state => state.PatientId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(state => state.QueueId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(state => state.RoomId)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(state => state.TrajectoryId)
            .HasDatabaseName(TrajectoryIdIndexName);

        builder.HasIndex(state => state.LastCorrelationId)
            .HasDatabaseName(LastCorrelationIdIndexName);

        builder.HasIndex(state => state.PatientId);
    }
}
