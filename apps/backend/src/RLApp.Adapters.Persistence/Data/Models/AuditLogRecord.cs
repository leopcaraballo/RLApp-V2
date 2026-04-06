using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RLApp.Adapters.Persistence.Data.Models;

/// <summary>
/// Persistence model for audit trail logs.
/// </summary>
public class AuditLogRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty; // JSON data
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public class Configuration : IEntityTypeConfiguration<AuditLogRecord>
    {
        public void Configure(EntityTypeBuilder<AuditLogRecord> builder)
        {
            builder.ToTable("audit_logs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Actor).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Entity).HasMaxLength(100).IsRequired();
            builder.Property(e => e.EntityId).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Payload).HasColumnType("jsonb");
            builder.Property(e => e.CorrelationId).HasMaxLength(100).IsRequired();

            builder.HasIndex(e => e.CorrelationId);
            builder.HasIndex(e => e.OccurredAt);
            builder.HasIndex(e => e.EntityId);
            builder.HasIndex(e => new { e.Entity, e.EntityId, e.OccurredAt });
            builder.HasIndex(e => new { e.Actor, e.Action, e.OccurredAt });
        }
    }
}
