using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RLApp.Adapters.Persistence.Data.Models;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AggregateId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty; // JSON blob
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }

    public class Configuration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.AggregateId).IsRequired().HasMaxLength(128);
            builder.Property(e => e.CorrelationId).IsRequired().HasMaxLength(128);
            builder.Property(e => e.Type).IsRequired().HasMaxLength(256);
            builder.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();
            builder.Property(e => e.ProcessedAt).IsRequired(false);
            builder.Property(e => e.AttemptCount).HasDefaultValue(0);

            builder.HasIndex(e => e.ProcessedAt);
            builder.HasIndex(e => e.OccurredAt);
            builder.HasIndex(e => e.CorrelationId);
            builder.HasIndex(e => new { e.AggregateId, e.OccurredAt });
        }
    }
}
