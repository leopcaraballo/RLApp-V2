using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RLApp.Adapters.Persistence.Data.Models;

public class OutboxDeadLetterMessage
{
    public Guid Id { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime FailedAt { get; set; }
    public string FailureReason { get; set; } = string.Empty;

    public class Configuration : IEntityTypeConfiguration<OutboxDeadLetterMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxDeadLetterMessage> builder)
        {
            builder.ToTable("OutboxDeadLetterMessages");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.AggregateId).IsRequired().HasMaxLength(128);
            builder.Property(entity => entity.CorrelationId).IsRequired().HasMaxLength(128);
            builder.Property(entity => entity.Type).IsRequired().HasMaxLength(256);
            builder.Property(entity => entity.Payload).HasColumnType("jsonb").IsRequired();
            builder.Property(entity => entity.FailureReason).IsRequired().HasColumnType("text");

            builder.HasIndex(entity => entity.CorrelationId);
            builder.HasIndex(entity => entity.FailedAt);
            builder.HasIndex(entity => new { entity.AggregateId, entity.FailedAt });
        }
    }
}
