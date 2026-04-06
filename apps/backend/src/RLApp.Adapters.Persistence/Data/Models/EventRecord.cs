using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RLApp.Adapters.Persistence.Data.Models;

public class EventRecord
{
    public const string AggregateSequenceIndexName = "IX_EventStore_AggregateId_SequenceNumber";

    public Guid Id { get; set; } = Guid.NewGuid();
    public string AggregateId { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty; // JSON Content
    public DateTime OccurredAt { get; set; }

    public class Configuration : IEntityTypeConfiguration<EventRecord>
    {
        public void Configure(EntityTypeBuilder<EventRecord> builder)
        {
            builder.ToTable("EventStore");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.AggregateId).IsRequired().HasMaxLength(128);
            builder.Property(e => e.SequenceNumber).IsRequired();
            builder.Property(e => e.EventType).IsRequired().HasMaxLength(256);
            builder.Property(e => e.CorrelationId).HasMaxLength(128);
            builder.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();

            builder.HasIndex(e => new { e.AggregateId, e.OccurredAt });
            builder.HasIndex(e => new { e.AggregateId, e.SequenceNumber })
                .IsUnique()
                .HasDatabaseName(AggregateSequenceIndexName);
            builder.HasIndex(e => e.CorrelationId);
            builder.HasIndex(e => new { e.EventType, e.OccurredAt });
        }
    }
}
